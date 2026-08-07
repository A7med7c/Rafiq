import { Injectable } from '@angular/core';
import { Capacitor } from '@capacitor/core';
import { LocalNotifications } from '@capacitor/local-notifications';
import { SQLiteConnection, SQLiteDBConnection } from '@capacitor-community/sqlite';
import { MedicationReminderNotificationPayload, AppointmentReminderNotificationPayload } from './signalr.service';
import { UpcomingReminderDto } from './medication-reminders.service';

/**
 * Row shape stored in the SQLite offline_reminders table.
 * notificationId is generated locally and kept stable across syncs —
 * we only reschedule a notification when the reminder actually changes.
 */
export interface CachedReminder {
  serverId: string;          // reminderId or appointmentId (PRIMARY KEY)
  notificationId: number;    // Capacitor local notification ID — stable across syncs
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
  private readonly tableName = 'reminders';
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
          serverId       TEXT    PRIMARY KEY,
          notificationId INTEGER NOT NULL,
          title          TEXT    NOT NULL,
          body           TEXT    NOT NULL,
          type           TEXT    NOT NULL,
          reminderTime   TEXT    NOT NULL,
          lastUpdated    TEXT    NOT NULL,
          status         TEXT    NOT NULL DEFAULT 'scheduled'
        );
      `);
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
    const localMap = new Map<string, CachedReminder>(local.map(r => [r.serverId, r]));
    const serverIds = new Set<string>();

    for (const item of serverItems) {
      serverIds.add(item.reminderId);
      const existing = localMap.get(item.reminderId);

      // (a) Server says deleted → cancel + remove
      if (item.isDeleted) {
        if (existing) {
          await this.cancelNotification(existing.notificationId);
          await this.deleteRow(item.reminderId);
        }
        continue;
      }

      if (!existing) {
        // (b) New reminder → insert + schedule
        await this.insertAndSchedule(item);
      } else if (existing.lastUpdated !== item.updatedAt) {
        // (c) Changed → update + reschedule using the SAME notificationId
        await this.updateAndReschedule(existing, item);
      } else if (existing.notificationId !== this.stableId(item.reminderId)) {
        await this.updateAndReschedule(existing, item);
      }
      // (d) Unchanged → leave untouched
    }

    // Step 3: remove local rows the server no longer returns
    for (const row of localMap.values()) {
      if (!serverIds.has(row.serverId)) {
        await this.cancelNotification(row.notificationId);
        await this.deleteRow(row.serverId);
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
        const notificationId = this.stableId(row.serverId);
        if (row.notificationId !== notificationId) {
          await this.cancelNotification(row.notificationId);
          await this.db.run(
            `UPDATE ${this.tableName} SET notificationId = ? WHERE serverId = ?`,
            [notificationId, row.serverId]
          );
        }

        await LocalNotifications.schedule({
          notifications: [{
            id: notificationId,
            title: row.title,
            body: row.body,
            schedule: { at: new Date(row.reminderTime) },
            extra: { sourceId: row.serverId, action: row.type === 'reminder' ? 'open-reminder' : 'open-appointment' },
          }],
        });
      } else {
        // Past reminder — clean up
        await this.deleteRow(row.serverId);
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

    // Check for existing row to reuse its notificationId
    const existing = await this.loadOne(serverId);
    const notificationId = this.stableId(serverId);

    // Cancel old notification before rescheduling
    if (existing) {
      await this.cancelNotification(notificationId);
    }

    await LocalNotifications.schedule({
      notifications: [{
        id: notificationId,
        title,
        body,
        schedule: { at: new Date(reminderTime) },
        extra: { sourceId: serverId, action: kind === 'reminder' ? 'open-reminder' : 'open-appointment' },
      }],
    });

    await this.db.run(
      `INSERT OR REPLACE INTO ${this.tableName}
         (serverId, notificationId, title, body, type, reminderTime, lastUpdated, status)
       VALUES (?, ?, ?, ?, ?, ?, ?, 'scheduled')`,
      [serverId, notificationId, title, body, kind, reminderTime, now]
    );
  }

  /**
   * Cancels and removes a reminder. Called by SignalR cancellation handlers.
   * Preserved for backward compatibility.
   */
  async removeReminder(serverId: string): Promise<void> {
    await this.init();
    const existing = await this.loadOne(serverId);
    if (existing) {
      await this.cancelNotification(existing.notificationId);
    }
    await this.deleteRow(serverId);
  }

  async getCachedReminderIds(): Promise<string[]> {
    await this.init();
    return (await this.loadAll()).map(row => row.serverId);
  }

  async getCachedReminders(): Promise<CachedReminder[]> {
    await this.init();
    return this.loadAll();
  }

  // ── Private helpers ────────────────────────────────────────────────────────

  private async insertAndSchedule(item: UpcomingReminderDto): Promise<void> {
    const notificationId = this.stableId(item.reminderId);
    const fireAt = new Date(item.scheduledAt);

    if (fireAt.getTime() > Date.now()) {
      await LocalNotifications.schedule({
        notifications: [{
          id: notificationId,
          title: item.title,
          body: item.body,
          schedule: { at: fireAt },
          extra: { sourceId: item.reminderId, action: item.reminderType === 'Medication' ? 'open-reminder' : 'open-appointment' },
        }],
      });
    }

    await this.db.run(
      `INSERT INTO ${this.tableName}
         (serverId, notificationId, title, body, type, reminderTime, lastUpdated, status)
       VALUES (?, ?, ?, ?, ?, ?, ?, 'scheduled')`,
      [item.reminderId, notificationId, item.title, item.body,
       item.reminderType === 'Medication' ? 'reminder' : 'appointment',
       item.scheduledAt, item.updatedAt]
    );
  }

  private async updateAndReschedule(existing: CachedReminder, item: UpcomingReminderDto): Promise<void> {
    const notificationId = this.stableId(item.reminderId);
    await this.cancelNotification(existing.notificationId);

    const fireAt = new Date(item.scheduledAt);
    if (fireAt.getTime() > Date.now()) {
      await LocalNotifications.schedule({
        notifications: [{
          id: notificationId,
          title: item.title,
          body: item.body,
          schedule: { at: fireAt },
          extra: { sourceId: item.reminderId, action: item.reminderType === 'Medication' ? 'open-reminder' : 'open-appointment' },
        }],
      });
    }

    await this.db.run(
      `UPDATE ${this.tableName}
          SET notificationId = ?, title = ?, body = ?, reminderTime = ?, lastUpdated = ?, status = 'scheduled'
        WHERE serverId = ?`,
      [notificationId, item.title, item.body, item.scheduledAt, item.updatedAt, item.reminderId]
    );
  }

  private async cancelNotification(notificationId: number): Promise<void> {
    try {
      await LocalNotifications.cancel({ notifications: [{ id: notificationId }] });
    } catch {
      // Notification may have already fired — safe to ignore
    }
  }

  private async deleteRow(serverId: string): Promise<void> {
    await this.db.run(`DELETE FROM ${this.tableName} WHERE serverId = ?`, [serverId]);
  }

  private async loadAll(): Promise<CachedReminder[]> {
    const result = await this.db.query(`SELECT * FROM ${this.tableName}`);
    return (result.values ?? []) as CachedReminder[];
  }

  private async loadOne(serverId: string): Promise<CachedReminder | undefined> {
    const result = await this.db.query(
      `SELECT * FROM ${this.tableName} WHERE serverId = ?`, [serverId]
    );
    return result.values?.[0] as CachedReminder | undefined;
  }

  private stableId(serverId: string): number {
    let hash = 0;
    for (let i = 0; i < serverId.length; i++) {
      hash = Math.imul(31, hash) + serverId.charCodeAt(i);
      hash |= 0;
    }

    const id = hash & 0x7fffffff;
    return id === 0 ? 1 : id;
  }
}
