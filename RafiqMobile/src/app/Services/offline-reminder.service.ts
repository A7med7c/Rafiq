import { Injectable } from '@angular/core';
import { Capacitor } from '@capacitor/core';
import { SQLiteConnection, SQLiteDBConnection } from '@capacitor-community/sqlite';
import { MedicationReminderNotificationPayload, AppointmentReminderNotificationPayload } from './signalr.service';
import { UpcomingReminderDto } from './medication-reminders.service';

/**
 * Row shape stored in the SQLite offline_reminders table.
 * notificationId is generated locally and kept stable across syncs —
 * we only reschedule a notification when the reminder actually changes.
 */
export interface CachedReminder {
  occurrenceKey: string;     // `${serverId}|${reminderTime}` — PRIMARY KEY (occurrence-unique)
  serverId: string;          // reminderId or appointmentId (shared across a reminder's occurrences)
  notificationId: number;    // deterministic occurrence id — stable across syncs
  title: string;
  body: string;
  type: 'reminder' | 'appointment';
  reminderTime: string;      // ISO-8601 scheduledAt
  lastUpdated: string;       // ISO-8601 updatedAt — sync cursor
  status: 'scheduled' | 'cancelled';
}

@Injectable({ providedIn: 'root' })
export class OfflineReminderService {
  private readonly sqlite = new SQLiteConnection(Capacitor);
  private db!: SQLiteDBConnection;
  private readonly dbName = 'offline_reminders';
  // v2: occurrence-keyed table. A medication reminder produces multiple daily
  // occurrences that share one serverId; keying only by serverId collapsed them
  // to a single row and a single alarm. The v2 table's PRIMARY KEY is the
  // occurrence key (`${serverId}|${reminderTime}`) so every occurrence survives
  // independently.
  private readonly tableName = 'reminders_v2';
  private readonly legacyTableName = 'reminders';
  private initialized = false;
  /** Guards against concurrent init() calls racing before initialized is set. */
  private initInFlight: Promise<void> | null = null;
  /** Concurrency guard: if a sync is already in flight, subsequent callers await it. */
  private syncInFlight: Promise<void> | null = null;

  constructor() {
    // Lazy init — called on first use to avoid race conditions at startup.
  }

  // ── Initialization ────────────────────────────────────────────────────────

  async init(): Promise<void> {
    if (this.initialized) return;
    // Concurrency guard: two concurrent callers share the same init Promise.
    if (this.initInFlight) return this.initInFlight;
    this.initInFlight = this._runInit().finally(() => {
      this.initInFlight = null;
    });
    return this.initInFlight;
  }

  private async _runInit(): Promise<void> {
    if (this.initialized) return; // re-check after acquiring the guard
    try {
      this.db = await this.sqlite.createConnection(this.dbName, false, 'no-encryption', 1, false);
      await this.db.open();
      await this.db.execute(`
        CREATE TABLE IF NOT EXISTS ${this.tableName} (
          occurrenceKey  TEXT    PRIMARY KEY,
          serverId       TEXT    NOT NULL,
          notificationId INTEGER NOT NULL,
          title          TEXT    NOT NULL,
          body           TEXT    NOT NULL,
          type           TEXT    NOT NULL,
          reminderTime   TEXT    NOT NULL,
          lastUpdated    TEXT    NOT NULL,
          status         TEXT    NOT NULL DEFAULT 'scheduled'
        );
        CREATE INDEX IF NOT EXISTS idx_${this.tableName}_serverId ON ${this.tableName}(serverId);
      `);
      // Drop the legacy serverId-keyed table so stale single-row-per-reminder
      // entries can't shadow the new occurrence rows.
      await this.db.execute(`DROP TABLE IF EXISTS ${this.legacyTableName};`);
      this.initialized = true;
    } catch (e) {
      console.error('[OfflineReminderService] init error', e);
      throw e;
    }
  }

  // ── Diff-based sync (called by ReminderBootstrapService) ──────────────────

  /**
   * Synchronizes the local SQLite cache with a combined server snapshot
   * (medication reminders + appointments merged into UpcomingReminderDto[]).
   *
   * Algorithm:
   *   • isDeleted=true  → cancel notification + delete row
   *   • not in SQLite   → insert + schedule notification
   *   • updatedAt changed → update row + reschedule notification (same notificationId)
   *   • unchanged       → no-op (notificationId stays stable, no unnecessary reschedule)
   *   • local row absent from server snapshot → cancel + delete
   *
   * The table is NEVER cleared — this keeps notification IDs stable.
   */
  async syncFromServer(serverItems: UpcomingReminderDto[]): Promise<void> {
    // Concurrency guard: never run two syncs simultaneously.
    // If one is in flight, the new caller awaits the same Promise.
    if (this.syncInFlight) {
      return this.syncInFlight;
    }
    this.syncInFlight = this._runSync(serverItems).finally(() => {
      this.syncInFlight = null;
    });
    return this.syncInFlight;
  }

  private async _runSync(serverItems: UpcomingReminderDto[]): Promise<void> {
    await this.init();

    const local = await this.loadAll();
    const localMap = new Map<string, CachedReminder>(local.map(r => [r.occurrenceKey, r]));
    const serverKeys = new Set<string>();

    for (const item of serverItems) {
      // (a) Server says deleted → remove EVERY occurrence of this reminder.
      if (item.isDeleted) {
        await this.deleteAllForServerId(item.reminderId);
        continue;
      }

      const occurrenceKey = this.occurrenceKey(item.reminderId, item.scheduledAt);
      serverKeys.add(occurrenceKey);
      const existing = localMap.get(occurrenceKey);

      if (!existing) {
        // (b) New occurrence → insert + schedule
        await this.insertAndSchedule(item);
      } else if (
        existing.lastUpdated !== item.updatedAt ||
        existing.notificationId !== this.stableId(occurrenceKey)
      ) {
        // (c) Changed → update the same occurrence row
        await this.updateAndReschedule(existing, item);
      }
      // (d) Unchanged → leave untouched
    }

    // Step 3: remove local occurrence rows the server no longer returns.
    for (const row of localMap.values()) {
      if (!serverKeys.has(row.occurrenceKey)) {
        await this.deleteRow(row.occurrenceKey);
      }
    }
  }

  // ── App-start restore ─────────────────────────────────────────────────────

  /**
   * Re-schedules any future notifications that may have been cleared by an OS
   * reboot or app restart. Safe to call unconditionally on every startup.
   */
  async restoreScheduledReminders(): Promise<void> {
    await this.init();

    const rows = await this.loadAll();
    const now = Date.now();

    for (const row of rows) {
      const fireAt = new Date(row.reminderTime).getTime();
      if (fireAt > now) {
        const notificationId = this.stableId(row.occurrenceKey);
        if (row.notificationId !== notificationId) {
          await this.db.run(
            `UPDATE ${this.tableName} SET notificationId = ? WHERE occurrenceKey = ?`,
            [notificationId, row.occurrenceKey]
          );
        }
      } else {
        // Past occurrence — clean up
        await this.deleteRow(row.occurrenceKey);
      }
    }
  }

  // ── Legacy SignalR-driven upsert (existing callers unchanged) ─────────────

  /**
   * Called by SignalR push handlers to immediately add or update a single reminder.
   * Preserved for backward compatibility with existing notification.service.ts callers.
   */
  async upsertReminder(
    payload: MedicationReminderNotificationPayload | AppointmentReminderNotificationPayload,
    kind: 'reminder' | 'appointment'
  ): Promise<void> {
    await this.init();

    const serverId = kind === 'reminder'
      ? (payload as MedicationReminderNotificationPayload).reminderId
      : (payload as AppointmentReminderNotificationPayload).appointmentId;
    const title = kind === 'reminder'
      ? (payload as MedicationReminderNotificationPayload).medicineName
      : (payload as AppointmentReminderNotificationPayload).title;
    const body = kind === 'reminder'
      ? ((payload as MedicationReminderNotificationPayload).notificationText ?? '')
      : ((payload as AppointmentReminderNotificationPayload).notificationText ?? '');
    const reminderTime = kind === 'reminder'
      ? (payload as MedicationReminderNotificationPayload).reminderTime
      : (payload as AppointmentReminderNotificationPayload).appointmentDateTime;
    const now = new Date().toISOString();

    const occurrenceKey = this.occurrenceKey(serverId, reminderTime);
    const notificationId = this.stableId(occurrenceKey);

    await this.db.run(
      `INSERT OR REPLACE INTO ${this.tableName}
         (occurrenceKey, serverId, notificationId, title, body, type, reminderTime, lastUpdated, status)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, 'scheduled')`,
      [occurrenceKey, serverId, notificationId, title, body, kind, reminderTime, now]
    );
  }

  /**
   * Cancels and removes a reminder (all of its occurrences). Called by SignalR
   * cancellation handlers and after an alarm action completes.
   * Preserved for backward compatibility.
   */
  async removeReminder(serverId: string): Promise<void> {
    await this.init();
    await this.deleteAllForServerId(serverId);
  }

  /** Returns the distinct reminder ids (not occurrence keys) currently cached. */
  async getCachedReminderIds(): Promise<string[]> {
    await this.init();
    return [...new Set((await this.loadAll()).map(row => row.serverId))];
  }

  async getCachedReminders(): Promise<CachedReminder[]> {
    await this.init();
    return this.loadAll();
  }

  // ── Private helpers ────────────────────────────────────────────────────────

  private async insertAndSchedule(item: UpcomingReminderDto): Promise<void> {
    const occurrenceKey = this.occurrenceKey(item.reminderId, item.scheduledAt);
    const notificationId = this.stableId(occurrenceKey);

    await this.db.run(
      `INSERT OR REPLACE INTO ${this.tableName}
         (occurrenceKey, serverId, notificationId, title, body, type, reminderTime, lastUpdated, status)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, 'scheduled')`,
      [occurrenceKey, item.reminderId, notificationId, item.title, item.body,
       item.reminderType === 'Medication' ? 'reminder' : 'appointment',
       item.scheduledAt, item.updatedAt]
    );
  }

  private async updateAndReschedule(existing: CachedReminder, item: UpcomingReminderDto): Promise<void> {
    const occurrenceKey = this.occurrenceKey(item.reminderId, item.scheduledAt);
    const notificationId = this.stableId(occurrenceKey);

    await this.db.run(
      `UPDATE ${this.tableName}
          SET notificationId = ?, title = ?, body = ?, reminderTime = ?, lastUpdated = ?, status = 'scheduled'
        WHERE occurrenceKey = ?`,
      [notificationId, item.title, item.body, item.scheduledAt, item.updatedAt, occurrenceKey]
    );
  }


  private async deleteRow(occurrenceKey: string): Promise<void> {
    await this.db.run(`DELETE FROM ${this.tableName} WHERE occurrenceKey = ?`, [occurrenceKey]);
  }

  private async deleteAllForServerId(serverId: string): Promise<void> {
    await this.db.run(`DELETE FROM ${this.tableName} WHERE serverId = ?`, [serverId]);
  }

  private async loadAll(): Promise<CachedReminder[]> {
    const result = await this.db.query(`SELECT * FROM ${this.tableName}`);
    return (result.values ?? []) as CachedReminder[];
  }

  /** Deterministic occurrence key: one reminder id yields one row per fire time. */
  private occurrenceKey(serverId: string, reminderTime: string): string {
    return `${serverId}|${reminderTime}`;
  }

  private stableId(source: string): number {
    let hash = 0;
    for (let i = 0; i < source.length; i++) {
      hash = Math.imul(31, hash) + source.charCodeAt(i);
      hash |= 0;
    }

    const id = hash & 0x7fffffff;
    return id === 0 ? 1 : id;
  }
}
