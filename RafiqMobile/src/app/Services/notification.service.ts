import { Injectable, NgZone, computed, effect, inject, isDevMode, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth-service';
import { AppointmentsService } from './appointments.service';
import { MedicationRemindersService } from './medication-reminders.service';
import { AppointmentReminderNotificationPayload, DocumentAnalysisCompletedPayload, DocumentAnalysisFailedPayload, MedicationReminderNotificationPayload, NotificationEventPayload, SignalRService } from './signalr.service';
import { NotificationSoundService } from './notification-sound.service';
import { PersistedNotificationsService } from './persisted-notifications.service';
import { LocalizationService } from './localization.service';

import { LocalNotifications } from '@capacitor/local-notifications';

export interface AppNotification {
  id: string;
  title: string;
  body: string;
  titleAr?: string;
  bodyAr?: string;
  type: 'appointment' | 'reminder' | 'confirmation' | 'system';
  createdAt: Date;
  read: boolean;
  sourceId?: string;
  /** Server-assigned ID for notifications persisted in the DB (e.g. review replies). */
  serverId?: string;
}



export interface BrowserNotificationItem {
  id: string;
  title: string;
  body: string;
  tag?: string;
  createdAt: Date;
  sourceId?: string;
}

export type NotificationCenterFilter = 'all' | 'unread' | 'read' | 'reminders' | 'confirmations';

interface StoredAppNotification {
  id: string;
  title: string;
  body: string;
  titleAr?: string;
  bodyAr?: string;
  type: AppNotification['type'];
  createdAt: string;
  read: boolean;
  sourceId?: string;
  serverId?: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private static readonly snoozeDelayMs = 10 * 60 * 1000;

  private readonly authService = inject(AuthService);
  private readonly signalr = inject(SignalRService);
  private readonly medicationRemindersService = inject(MedicationRemindersService);
  private readonly appointmentsService = inject(AppointmentsService);
  private readonly notificationSoundService = inject(NotificationSoundService);
  private readonly persistedSvc = inject(PersistedNotificationsService);

  private readonly localization = inject(LocalizationService);

  private readonly router = inject(Router);
  private readonly ngZone = inject(NgZone);

  private readonly _notifications = signal<AppNotification[]>([]);
  private readonly _reminderQueue = signal<MedicationReminderNotificationPayload[]>([]);
  private readonly _browserNotifications = signal<BrowserNotificationItem[]>([]);
  private readonly _reminderModalOpen = signal(false);
  private readonly _notificationCenterOpen = signal(false);
  private readonly _browserNotificationPermission = signal<NotificationPermission>(this.readBrowserNotificationPermission());
  private readonly _confirmingReminder = signal(false);
  private readonly _confirmingAppointment = signal(false);
  private readonly _reminderDataRefreshTick = signal(0);
  private readonly _appointmentReminderQueue = signal<AppointmentReminderNotificationPayload[]>([]);
  private readonly _appointmentReminderModalOpen = signal(false);
  private readonly _appointmentDataRefreshTick = signal(0);

  private readonly snoozeTimers = new Map<string, ReturnType<typeof setTimeout>>();
  private readonly browserNotificationKeys = new Set<string>();
  private readonly processedReminderIds = new Set<string>();
  private readonly promptedPermissionUsers = new Set<string>();
  private lastKnownUserId: string | null = this.authService.currentUser?.userId ?? null;
  private persistenceKey = this.getPersistenceKey(this.lastKnownUserId);
  private suppressPersistence = false;

  readonly notifications = this._notifications.asReadonly();
  readonly unreadNotifications = computed(() => this._notifications().filter(notification => !notification.read));
  readonly readNotifications = computed(() => this._notifications().filter(notification => notification.read));
  readonly reminderNotifications = computed(() => this._notifications().filter(notification => notification.type === 'reminder'));
  readonly confirmationNotifications = computed(() => this._notifications().filter(notification => notification.type === 'confirmation'));
  readonly unreadCount = computed(() => this.unreadNotifications().length);
  readonly badgeCount = this.unreadCount;
  readonly reminderQueue = this._reminderQueue.asReadonly();
  readonly nextReminder = computed(() => this._reminderQueue()[0] ?? null);
  readonly activeReminder = this.nextReminder;
  readonly reminderModalOpen = this._reminderModalOpen.asReadonly();
  readonly confirmingReminder = this._confirmingReminder.asReadonly();
  readonly confirmingAppointment = this._confirmingAppointment.asReadonly();
  readonly browserNotifications = this._browserNotifications.asReadonly();
  readonly notificationCenterOpen = this._notificationCenterOpen.asReadonly();
  readonly browserNotificationPermission = this._browserNotificationPermission.asReadonly();
  readonly reminderDataRefreshTick = this._reminderDataRefreshTick.asReadonly();
  readonly appointmentReminderQueue = this._appointmentReminderQueue.asReadonly();
  readonly activeAppointmentReminder = computed(() => this._appointmentReminderQueue()[0] ?? null);
  readonly appointmentReminderModalOpen = this._appointmentReminderModalOpen.asReadonly();
  readonly appointmentDataRefreshTick = this._appointmentDataRefreshTick.asReadonly();
  readonly centerFilter = signal<NotificationCenterFilter>('all');
  readonly visibleNotifications = computed(() => {
    const filter = this.centerFilter();
    const isAr = this.localization.lang() === 'ar';
    const list = this.notifications().map(n => isAr
      ? { ...n, title: n.titleAr ?? n.title, body: n.bodyAr ?? n.body }
      : n
    );

    switch (filter) {
      case 'unread':
        return list.filter(n => !n.read);
      case 'read':
        return list.filter(n => n.read);
      case 'reminders':
        return list.filter(n => n.type === 'reminder');
      case 'confirmations':
        return list.filter(n => n.type === 'confirmation');
      default:
        return list;
    }
  });

  private readonly _initLocalNotifications = () => {
    LocalNotifications.addListener('localNotificationActionPerformed', (notificationAction) => {
      this.ngZone.run(() => {
        const payload = notificationAction.notification.extra;
        if (payload && payload.action === 'open-reminder' && payload.sourceId) {
          this.openReminderFromToast(payload.sourceId);
        } else if (payload && payload.action === 'open-appointment' && payload.sourceId) {
          // You could open the appointment directly, for now we let the app handle it
        }
      });
    });
  };

  constructor() {
    this._initLocalNotifications();
    
    this.authService.currentUser$.subscribe((user) => {
      const nextUserId = user?.userId ?? null;

      if (nextUserId && !this.promptedPermissionUsers.has(nextUserId)) {
        this.promptedPermissionUsers.add(nextUserId);
        void this.requestBrowserNotificationPermission();
      }

      if (this.lastKnownUserId !== nextUserId) {
        this.resetSessionState();
        this.persistenceKey = this.getPersistenceKey(nextUserId);
        this.suppressPersistence = true;
        this._notifications.set([]);
        this.suppressPersistence = false;
        this.loadPersistedNotifications();
        if (nextUserId) {
          this.loadServerNotifications();
        }
      }

      this.lastKnownUserId = nextUserId;
    });

    this.loadPersistedNotifications();

    effect(() => {
      if (this.suppressPersistence) {
        return;
      }

      this.persistNotifications();
    });

    effect(() => {
      const reminderEvents             = this.signalr.reminderEvents();
      const notificationEvents         = this.signalr.notificationEvents();
      const appointmentReminderEvents  = this.signalr.appointmentReminderEvents();
      const docCompletedEvents         = this.signalr.documentAnalysisCompletedEvents();
      const docFailedEvents            = this.signalr.documentAnalysisFailedEvents();

      if (!reminderEvents.length && !notificationEvents.length && !appointmentReminderEvents.length
          && !docCompletedEvents.length && !docFailedEvents.length) {
        return;
      }

      if (reminderEvents.length) {
        this.ingestReminderEvents(this.signalr.drainReminderEvents());
      }

      if (notificationEvents.length) {
        this.ingestNotificationEvents(this.signalr.drainNotificationEvents());
      }

      if (appointmentReminderEvents.length) {
        this.ingestAppointmentReminderEvents(this.signalr.drainAppointmentReminderEvents());
      }

      if (docCompletedEvents.length) {
        const events = this.signalr.drainDocumentAnalysisCompletedEvents();
        const t = this.localization.t().documentAnalysis;
        events.forEach(e => {
          this.emitNativeNotification({
            id: crypto.randomUUID(),
            title: `${t.analysisComplete}: ${e.title}`,
            body: t.analysisCompleteBody,
            createdAt: new Date(),
          });
        });
      }

      if (docFailedEvents.length) {
        const events = this.signalr.drainDocumentAnalysisFailedEvents();
        const t = this.localization.t().documentAnalysis;
        events.forEach(e => {
          this.emitNativeNotification({
            id: crypto.randomUUID(),
            title: `${t.analysisFailed}: ${e.title}`,
            body: e.failureReason || t.analysisFailedBody,
            createdAt: new Date(),
          });
        });
      }
    });
  }

  push(notification: Omit<AppNotification, 'id' | 'createdAt' | 'read'>): void {
    const entry: AppNotification = {
      ...notification,
      id: crypto.randomUUID(),
      createdAt: new Date(),
      read: false,
    };

    this._notifications.update(list => [entry, ...list]);
  }

  markAllRead(): void {
    this._notifications.update(list => list.map(notification => ({ ...notification, read: true })));
    this.persistedSvc.markAllRead().subscribe();
  }

  markRead(id: string): void {
    const target = this._notifications().find(n => n.id === id);
    this._notifications.update(list =>
      list.map(notification => (notification.id === id ? { ...notification, read: true } : notification))
    );
    if (target?.serverId) {
      this.persistedSvc.markRead(target.serverId).subscribe();
    }
  }

  markReadBySourceId(sourceId: string): void {
    this._notifications.update(list =>
      list.map(notification =>
        notification.sourceId === sourceId ? { ...notification, read: true } : notification
      )
    );
  }

  openNotificationCenter(): void {
    this._notificationCenterOpen.set(true);
  }

  closeNotificationCenter(): void {
    this._notificationCenterOpen.set(false);
  }

  toggleNotificationCenter(): void {
    this._notificationCenterOpen.update(isOpen => !isOpen);
  }

  acknowledgeReminder(reminderId?: string): void {
    const queue = this._reminderQueue();

    if (!queue.length) {
      this._reminderModalOpen.set(false);
      return;
    }

    const targetId = reminderId ?? queue[0].reminderId;
    const remainingQueue = queue.filter(reminder => reminder.reminderId !== targetId);

    if (remainingQueue.length === queue.length) {
      return;
    }

    this._reminderQueue.set(remainingQueue);
    this.markReadBySourceId(targetId);

    if (remainingQueue.length > 0) {
      this._reminderModalOpen.set(true);
    } else {
      this._reminderModalOpen.set(false);
    }
  }

  dismissReminder(reminderId?: string): void {
    const removed = this.dequeueReminder(reminderId);
    if (removed) {
      this.clearSnoozeTimer(removed.reminderId);
      this.markReadBySourceId(removed.reminderId);
    }
    this.closeReminderModalIfEmpty();
  }

  takeCurrentReminder(): void {
    const reminder = this.activeReminder();
    if (!reminder || this._confirmingReminder()) {
      this.closeReminderModalIfEmpty();
      return;
    }

    this._confirmingReminder.set(true);

    this.medicationRemindersService.confirm(reminder.reminderId).subscribe({
      next: res => {
        // Either this request just confirmed it, or the backend reports it was already
        // confirmed/cancelled elsewhere (stale popup, duplicate click, another tab) — in
        // both cases the reminder is done and this popup must not offer to confirm it again.
        this.clearSnoozeTimer(reminder.reminderId);
        this.dequeueReminder(reminder.reminderId);
        this.markReadBySourceId(reminder.reminderId);

        if (!res.success) {
          const nc = this.localization.t().notifications;
                  } else {
          const nc = this.localization.t().notifications;
          this.pushDerivedNotification({
            title: nc.medicationConfirmed,
            body: reminder.notificationText || nc.confirmedTakingBody.replace('{name}', reminder.medicineName),
            type: 'confirmation',
            sourceId: reminder.reminderId,
          });
          const ncAfter = this.localization.t().notifications;
                  }

        this.notifyReminderChanged();
        this._confirmingReminder.set(false);
        this.closeReminderModalIfEmpty();
      },
      error: err => {
        this._confirmingReminder.set(false);
        const nc = this.localization.t().notifications;
              },
    });
  }

  remindMeLater(): void {
    const reminder = this.activeReminder();
    if (!reminder) {
      this.closeReminderModalIfEmpty();
      return;
    }

    this.dequeueReminder(reminder.reminderId);
    this.scheduleSnoozedReminder(reminder);
    this.closeReminderModalIfEmpty();
  }



  requestBrowserNotificationPermission(): Promise<NotificationPermission> {
    if (typeof window === 'undefined' || !('Notification' in window)) {
      this._browserNotificationPermission.set('denied');
      return Promise.resolve('denied');
    }

    const currentPermission = Notification.permission;
    this._browserNotificationPermission.set(currentPermission);

    if (currentPermission !== 'default') {
      if (currentPermission === 'denied') {
        this._browserNotifications.set([]);
      }

      return Promise.resolve(currentPermission);
    }

    return Notification.requestPermission().then(permission => {
      this._browserNotificationPermission.set(permission);
      if (permission === 'granted') {
        this.flushBrowserNotifications();
      } else if (permission === 'denied') {
        this._browserNotifications.set([]);
      }
      return permission;
    });
  }

  flushBrowserNotifications(): void {
    if (!this.canShowBrowserNotifications()) {
      return;
    }

    const pending = this._browserNotifications();
    for (const notification of pending) {
      this.emitBrowserNotification(notification);
    }

    this._browserNotifications.set([]);
  }

  clearBrowserNotifications(): void {
    this._browserNotifications.set([]);
  }

  private ingestReminderEvents(reminders: MedicationReminderNotificationPayload[]): void {
    for (const reminder of reminders) {
      this.recordReminder(reminder);
    }
  }

  private ingestNotificationEvents(events: NotificationEventPayload[]): void {
    for (const event of events) {
      this.recordNotificationEvent(event);
    }
  }

  private ingestAppointmentReminderEvents(events: AppointmentReminderNotificationPayload[]): void {
    for (const event of events) {
      this.recordAppointmentReminder(event);
    }
  }

  private recordReminder(reminder: MedicationReminderNotificationPayload): void {
    if (this.processedReminderIds.has(reminder.reminderId)) {
      return;
    }

    this.processedReminderIds.add(reminder.reminderId);

    const nc = this.localization.t().notifications;
    const notification = this.pushDerivedNotification({
      title: reminder.medicineName,
      body: reminder.notificationText || nc.medicationReminderBody.replace('{name}', reminder.medicineName),
      type: 'reminder',
      sourceId: reminder.reminderId,
    });

    this._reminderQueue.update(queue => [...queue, reminder]);
    this._reminderModalOpen.set(true);

    this.emitNativeNotification({
      id: crypto.randomUUID(),
      title: this.localization.t().notifications.medicationReminderToast,
      body: `${reminder.medicineName} • ${reminder.reminderTime}`,
      createdAt: new Date(),
      sourceId: reminder.reminderId,
      action: 'open-reminder'
    });

    this.showBrowserReminderNotification(reminder);
    this.notificationSoundService.play();


    // The Hangfire job that fired this SignalR event has also just transitioned the backend
    // log from Pending → Sent.  Increment the refresh tick so the Medications page re-fetches
    // todayLogs and picks up the updated statuses, attempt states, and sort order — without
    // requiring a page reload.
    this.notifyReminderChanged();

    if (isDevMode()) {
      console.debug('[NotificationService] MedicationReminderDue received', reminder);
    }
  }

  private recordNotificationEvent(event: NotificationEventPayload): void {
    const isAr = this.localization.lang() === 'ar';
    const notification = this.pushDerivedNotification({
      title: event.title,
      body: event.message,
      titleAr: event.titleAr,
      bodyAr: event.bodyAr,
      type: 'system',
    });

    const displayTitle = isAr ? (event.titleAr ?? notification.title) : notification.title;
    const displayBody  = isAr ? (event.bodyAr  ?? notification.body)  : notification.body;
    
    this.emitNativeNotification({
      id: crypto.randomUUID(),
      title: displayTitle,
      body: displayBody,
      createdAt: new Date()
    });
  }

  private recordAppointmentReminder(event: AppointmentReminderNotificationPayload): void {
    if (this.processedReminderIds.has(event.appointmentId)) {
      return;
    }

    this.processedReminderIds.add(event.appointmentId);

    const nc = this.localization.t().notifications;
    this.pushDerivedNotification({
      title: nc.appointmentReminder,
      body: event.notificationText || nc.upcomingAppointmentBody.replace('{title}', event.title).replace('{provider}', event.provider),
      titleAr: nc.appointmentReminder,
      type: 'appointment',
      sourceId: event.appointmentId,
    });

    this._appointmentReminderQueue.update(q => [...q, event]);

    this.emitNativeNotification({
      id: crypto.randomUUID(),
      title: event.title,
      body: event.notificationText || nc.appointmentWithProviderBody.replace('{title}', event.title).replace('{provider}', event.provider),
      createdAt: new Date(),
      sourceId: event.appointmentId,
      action: 'open-appointment'
    });



    this.notificationSoundService.play();


    if (isDevMode()) {
      console.debug('[NotificationService] AppointmentReminderDue received', event);
    }
  }

  private pushDerivedNotification(notification: Omit<AppNotification, 'id' | 'createdAt' | 'read'>): AppNotification {
    const entry: AppNotification = {
      ...notification,
      id: crypto.randomUUID(),
      createdAt: new Date(),
      read: false,
    };

    this._notifications.update(list => [entry, ...list]);
    return entry;
  }

  private showBrowserReminderNotification(reminder: MedicationReminderNotificationPayload): void {
    const key = this.browserNotificationKey(reminder.reminderId);

    if (this.browserNotificationKeys.has(key)) {
      return;
    }

    const nc = this.localization.t().notifications;
    const entry: BrowserNotificationItem = {
      id: crypto.randomUUID(),
      title: reminder.medicineName,
      body: reminder.notificationText || nc.medicationReminderBody.replace('{name}', reminder.medicineName),
      createdAt: new Date(),
      sourceId: reminder.reminderId,
    };

    this.browserNotificationKeys.add(key);

    if (this.canShowBrowserNotifications()) {
      this.emitBrowserNotification(entry);
      return;
    }

    if (this._browserNotificationPermission() === 'default') {
      this._browserNotifications.update(list => [entry, ...list]);
    }

    if (typeof window !== 'undefined' && 'Notification' in window && Notification.permission === 'default') {
      void this.requestBrowserNotificationPermission();
    }
  }

  private emitNativeNotification(notification: BrowserNotificationItem & { action?: string }): void {
    const idNum = this.stableNotificationId(notification.sourceId ?? notification.id);
    
    LocalNotifications.schedule({
      notifications: [
        {
          id: idNum,
          title: notification.title,
          body: notification.body,
          extra: {
            sourceId: notification.sourceId,
            action: notification.action
          }
        }
      ]
    });
  }

  private stableNotificationId(source: string): number {
    let hash = 0;
    for (let i = 0; i < source.length; i++) {
      hash = Math.imul(31, hash) + source.charCodeAt(i);
      hash |= 0;
    }

    const id = hash & 0x7fffffff;
    return id === 0 ? 1 : id;
  }

  private emitBrowserNotification(notification: BrowserNotificationItem): void {
    if (!this.canShowBrowserNotifications()) {
      return;
    }

    const browserNotification = new Notification(notification.title, {
      body: notification.body,
      tag: notification.sourceId ?? notification.id,
    });

    browserNotification.onclick = () => {
      this.ngZone.run(() => {
        if (typeof window !== 'undefined') {
          window.focus();
        }

        const reminderId = notification.sourceId;
        if (reminderId) {
          void this.router.navigate(['/medications'], {
            queryParams: {
              tab: 'schedule',
              reminderId,
            }
          });
        }
      });

      browserNotification.close();
    };
  }

  private canShowBrowserNotifications(): boolean {
    return typeof window !== 'undefined'
      && 'Notification' in window
      && Notification.permission === 'granted';
  }

  private readBrowserNotificationPermission(): NotificationPermission {
    if (typeof window === 'undefined' || !('Notification' in window)) {
      return 'denied';
    }

    return Notification.permission;
  }

  private browserNotificationKey(sourceId: string): string {
    return `reminder:${sourceId}`;
  }

  markNotificationRead(id: string): void {
    this.markRead(id);
  }

  setCenterFilter(filter: NotificationCenterFilter): void {
    this.centerFilter.set(filter);
  }

  openNotification(notification: AppNotification): void {
    this.markRead(notification.id);
    if (notification.sourceId) {
      this.markReadBySourceId(notification.sourceId);
    }
    this.closeNotificationCenter();

    if (notification.type === 'reminder' || notification.type === 'confirmation') {
      if (notification.sourceId) {
        void this.router.navigate(['/medications'], {
          queryParams: {
            tab: 'schedule',
            reminderId: notification.sourceId,
          },
        });
      }
      return;
    }

    if (notification.type === 'appointment') {
      void this.router.navigate(['/appointments']);
      return;
    }

    void this.router.navigate(['/dashboard']);
  }

  openReminderFromToast(reminderId: string): void {
    const queue = this._reminderQueue();
    const target = queue.find(reminder => reminder.reminderId === reminderId);

    if (!target) {
      return;
    }

    const nextQueue = [
      target,
      ...queue.filter(reminder => reminder.reminderId !== reminderId),
    ];

    this._reminderQueue.set(nextQueue);
    this.markReadBySourceId(reminderId);
    this._reminderModalOpen.set(true);
  }

  private dequeueReminder(reminderId?: string): MedicationReminderNotificationPayload | null {
    const queue = this._reminderQueue();

    if (!queue.length) {
      this._reminderQueue.set([]);
      return null;
    }

    const targetId = reminderId ?? queue[0].reminderId;
    const remainingQueue = queue.filter(reminder => reminder.reminderId !== targetId);
    const removed = queue.find(reminder => reminder.reminderId === targetId) ?? null;

    if (!removed) {
      return null;
    }

    this._reminderQueue.set(remainingQueue);
    return removed;
  }

  private scheduleSnoozedReminder(reminder: MedicationReminderNotificationPayload): void {
    this.clearSnoozeTimer(reminder.reminderId);

    const timer = setTimeout(() => {
      this.snoozeTimers.delete(reminder.reminderId);

      if (this._reminderQueue().some(item => item.reminderId === reminder.reminderId)) {
        return;
      }

      this._reminderQueue.update(queue => [reminder, ...queue]);
      this._reminderModalOpen.set(true);
    }, NotificationService.snoozeDelayMs);

    this.snoozeTimers.set(reminder.reminderId, timer);
  }

  private clearSnoozeTimer(reminderId: string): void {
    const timer = this.snoozeTimers.get(reminderId);
    if (!timer) {
      return;
    }

    clearTimeout(timer);
    this.snoozeTimers.delete(reminderId);
  }

  private closeReminderModalIfEmpty(): void {
    this._reminderModalOpen.set(this._reminderQueue().length > 0);
  }

  notifyReminderChanged(): void {
    this._reminderDataRefreshTick.update(tick => tick + 1);
  }

  openAppointmentReminderFromToast(appointmentId: string): void {
    const queue = this._appointmentReminderQueue();
    const target = queue.find(a => a.appointmentId === appointmentId);
    if (!target) return;
    this._appointmentReminderQueue.set([target, ...queue.filter(a => a.appointmentId !== appointmentId)]);
    this._appointmentReminderModalOpen.set(true);
  }

  /** "Confirm Attendance" — mark the appointment as Completed and dismiss the reminder toast. */
  acknowledgeAppointmentReminder(appointmentId: string): void {
    const queue = this._appointmentReminderQueue();
    const reminder = queue.find(r => r.appointmentId === appointmentId);
    if (!reminder || this._confirmingAppointment()) return;

    this._confirmingAppointment.set(true);

    this.appointmentsService.complete(appointmentId).subscribe({
      next: () => {
        this.clearSnoozeTimer(appointmentId);
        this._appointmentReminderQueue.update(q => q.filter(r => r.appointmentId !== appointmentId));
        this.markReadBySourceId(appointmentId);

        const nc = this.localization.t().notifications;
        this.pushDerivedNotification({
          title: nc.appointmentConfirmedNotifTitle,
          body: nc.appointmentConfirmedNotifBody
            .replace('{title}', reminder.title)
            .replace('{provider}', reminder.provider),
          type: 'confirmation',
          sourceId: appointmentId,
        });

                this._confirmingAppointment.set(false);
        this.notifyAppointmentChanged();
      },
      error: err => {
        this._confirmingAppointment.set(false);
        const nc = this.localization.t().notifications;
        this.emitNativeNotification({
          id: crypto.randomUUID(),
          title: nc.errorTitle,
          body: nc.failedToConfirmAttendanceBody,
          createdAt: new Date(),
        });
      },
    });
  }



  /** "Remind Me Later" — snooze and re-show the reminder after 10 minutes. */
  remindAppointmentLater(): void {
    const reminder = this.activeAppointmentReminder();
    if (!reminder) {
      this.closeAppointmentReminderModalIfEmpty();
      return;
    }

    this.dequeueAppointmentReminder(reminder.appointmentId);
    this.scheduleAppointmentSnooze(reminder);
    this.closeAppointmentReminderModalIfEmpty();
  }

  /** "Dismiss" — close the modal without any further action. */
  dismissAppointmentReminder(appointmentId?: string): void {
    const queue = this._appointmentReminderQueue();
    const targetId = appointmentId ?? queue[0]?.appointmentId;
    if (!targetId) return;

    this.clearSnoozeTimer(targetId);
    this._appointmentReminderQueue.update(q => q.filter(a => a.appointmentId !== targetId));
    this.markReadBySourceId(targetId);
    this.closeAppointmentReminderModalIfEmpty();
  }

  notifyAppointmentChanged(): void {
    this._appointmentDataRefreshTick.update(t => t + 1);
  }

  private dequeueAppointmentReminder(appointmentId: string): AppointmentReminderNotificationPayload | null {
    const queue = this._appointmentReminderQueue();
    const removed = queue.find(a => a.appointmentId === appointmentId) ?? null;
    if (!removed) return null;
    this._appointmentReminderQueue.update(q => q.filter(a => a.appointmentId !== appointmentId));
    return removed;
  }

  private closeAppointmentReminderModalIfEmpty(): void {
    this._appointmentReminderModalOpen.set(this._appointmentReminderQueue().length > 0);
  }

  private scheduleAppointmentSnooze(reminder: AppointmentReminderNotificationPayload): void {
    this.clearSnoozeTimer(reminder.appointmentId);

    const timer = setTimeout(() => {
      this.snoozeTimers.delete(reminder.appointmentId);

      if (this._appointmentReminderQueue().some(a => a.appointmentId === reminder.appointmentId)) {
        return;
      }

      this._appointmentReminderQueue.update(q => [reminder, ...q]);
      this._appointmentReminderModalOpen.set(true);
      this.notificationSoundService.play();
    }, NotificationService.snoozeDelayMs);

    this.snoozeTimers.set(reminder.appointmentId, timer);
  }

  private persistNotifications(): void {
    if (!this.persistenceKey || typeof window === 'undefined') {
      return;
    }

    const payload: StoredAppNotification[] = this._notifications().map(notification => ({
      id: notification.id,
      title: notification.title,
      body: notification.body,
      titleAr: notification.titleAr,
      bodyAr: notification.bodyAr,
      type: notification.type,
      createdAt: notification.createdAt.toISOString(),
      read: notification.read,
      sourceId: notification.sourceId,
      serverId: notification.serverId,
    }));

    window.localStorage.setItem(this.persistenceKey, JSON.stringify(payload));
  }

  private loadPersistedNotifications(): void {
    if (typeof window === 'undefined' || !this.persistenceKey) {
      return;
    }

    const raw = window.localStorage.getItem(this.persistenceKey);
    if (!raw) {
      return;
    }

    try {
      const parsed = JSON.parse(raw) as StoredAppNotification[];
      const notifications = parsed.map(notification => ({
        id: notification.id,
        title: notification.title,
        body: notification.body,
        titleAr: notification.titleAr,
        bodyAr: notification.bodyAr,
        type: notification.type,
        createdAt: new Date(notification.createdAt),
        read: notification.read,
        sourceId: notification.sourceId,
        serverId: notification.serverId,
      }));

      this.suppressPersistence = true;
      this._notifications.set(notifications);
    } catch {
      // Ignore malformed persisted history and keep the in-memory state.
    } finally {
      this.suppressPersistence = false;
    }
  }

  private loadServerNotifications(): void {
    this.persistedSvc.getAll(1, 50).subscribe({
      next: result => {
        const existing = this._notifications();
        const existingServerIds = new Set(existing.map(n => n.serverId).filter(Boolean));

        const incoming: AppNotification[] = result.items
          .filter(n => !existingServerIds.has(n.id))
          .map(n => ({
            id: crypto.randomUUID(),
            serverId: n.id,
            title: n.title,
            body: n.body,
            titleAr: n.titleAr ?? undefined,
            bodyAr: n.bodyAr ?? undefined,
            type: 'system' as const,
            createdAt: new Date(n.createdAt),
            read: n.isRead,
          }));

        if (incoming.length === 0) return;

        this._notifications.update(list => {
          const merged = [...list, ...incoming];
          merged.sort((a, b) => b.createdAt.getTime() - a.createdAt.getTime());
          return merged;
        });
      },
      error: () => { /* silently ignore if not authenticated yet */ }
    });
  }

  private getPersistenceKey(userId: string | null): string {
    return `rafiq.notifications.${userId ?? 'anonymous'}`;
  }

  private resetSessionState(): void {
    this.suppressPersistence = true;
    this._reminderQueue.set([]);
    this._browserNotifications.set([]);
    this._reminderModalOpen.set(false);
    this._notificationCenterOpen.set(false);
    this._confirmingReminder.set(false);
    this._confirmingAppointment.set(false);
    this._appointmentReminderQueue.set([]);
    this._appointmentReminderModalOpen.set(false);
    this.snoozeTimers.forEach(timer => clearTimeout(timer));
    this.snoozeTimers.clear();
    this.browserNotificationKeys.clear();
    this.processedReminderIds.clear();
    this.centerFilter.set('all');
    this.suppressPersistence = false;
  }
}
