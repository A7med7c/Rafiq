import { Injectable, inject, isDevMode, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './auth-service';
import { environment } from '../Environments/Environment';

export interface MedicationReminderNotificationPayload {
  reminderId: string;
  medicineId: string;
  medicineName: string;
  genericName: string;
  strength: string;
  dosage: string;
  reminderTime: string;
  status: string;
  notificationText: string;
}

export interface NotificationEventPayload {
  title: string;
  message: string;
}

export interface AppointmentReminderNotificationPayload {
  appointmentId: string;
  title: string;
  provider: string;
  appointmentDateTime: string;
  notificationText: string;
  appointmentType: string;
  customType?: string;
}

export type ConnectionState = 'Disconnected' | 'Connecting' | 'Connected' | 'Reconnecting';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private readonly authService = inject(AuthService);

  private connection: signalR.HubConnection | null = null;
  private starting = false;

  readonly connectionState = signal<ConnectionState>('Disconnected');
  readonly reminderEvents = signal<MedicationReminderNotificationPayload[]>([]);
  readonly notificationEvents = signal<NotificationEventPayload[]>([]);
  readonly appointmentReminderEvents = signal<AppointmentReminderNotificationPayload[]>([]);

  constructor() {
    // Connect only while a user is signed in; never negotiate anonymously.
    this.authService.currentUser$.subscribe((user) => {
      if (user) {
        void this.startConnection();
      } else {
        void this.stopConnection();
      }
    });

    // A refreshed access token means any connection that dropped on a dead token can come back.
    this.authService.tokensRefreshed$.subscribe(() => {
      if (this.authService.currentUser && this.connectionState() === 'Disconnected') {
        this.log('Token refreshed while disconnected — reconnecting.');
        void this.startConnection();
      }
    });
  }

  private async startConnection(): Promise<void> {
    if (this.connection || this.starting) {
      return;
    }

    this.starting = true;

    // Convert API URL (e.g. http://localhost:5000/api) to Hub URL (e.g. http://localhost:5000/hubs/notifications)
    const hubUrl = `${environment.apiUrl.replace('/api', '')}/hubs/notifications`;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        /**
         * Called on every negotiate AND on every automatic-reconnect attempt.
         * Never cache the result here: ask AuthService each time, which refreshes
         * the token first if it has expired.
         */
        accessTokenFactory: () => this.authService.getValidAccessToken()
      })
      .withAutomaticReconnect()
      .configureLogging(isDevMode() ? signalR.LogLevel.Information : signalR.LogLevel.Error)
      .build();

    this.setConnectionState('Connecting');

    // Register event listeners
    this.connection.on('MedicationReminderDue', (payload: MedicationReminderNotificationPayload) => {
      this.reminderEvents.update((currentQueue) => [...currentQueue, payload]);
    });

    this.connection.on('ReceiveNotification', (title: string, message: string) => {
      this.notificationEvents.update((currentQueue) => [...currentQueue, { title, message }]);
    });

    this.connection.on('AppointmentReminderDue', (payload: AppointmentReminderNotificationPayload) => {
      this.appointmentReminderEvents.update((currentQueue) => [...currentQueue, payload]);
    });

    this.connection.onclose(() => {
      this.log('Disconnected.');
      this.setConnectionState('Disconnected');
    });

    this.connection.onreconnecting(() => {
      this.log('Reconnecting…');
      this.setConnectionState('Reconnecting');
    });

    this.connection.onreconnected(() => {
      this.log('Reconnected.');
      this.setConnectionState('Connected');
    });

    this.log(`Starting connection to ${hubUrl}`);

    try {
      await this.connection.start();
      this.log('Connected.');
      this.setConnectionState('Connected');
    } catch (err) {
      console.error('SignalR connection error: ', err);

      // Drop the dead connection so a later attempt can build a fresh one.
      this.connection = null;
      this.setConnectionState('Disconnected');
    } finally {
      this.starting = false;
    }
  }

  private async stopConnection(): Promise<void> {
    if (!this.connection) {
      return;
    }

    const connection = this.connection;
    this.connection = null;

    try {
      await connection.stop();
      this.log('Stopped (signed out).');
    } catch (err) {
      console.error('Error stopping SignalR connection: ', err);
    } finally {
      this.setConnectionState('Disconnected');
    }
  }

  private log(message: string): void {
    if (isDevMode()) {
      console.debug(`[SignalR] ${message}`);
    }
  }

  drainReminderEvents(): MedicationReminderNotificationPayload[] {
    const events = this.reminderEvents();
    if (!events.length) {
      return [];
    }

    this.reminderEvents.set([]);
    return events;
  }

  drainNotificationEvents(): NotificationEventPayload[] {
    const events = this.notificationEvents();
    if (!events.length) {
      return [];
    }

    this.notificationEvents.set([]);
    return events;
  }

  drainAppointmentReminderEvents(): AppointmentReminderNotificationPayload[] {
    const events = this.appointmentReminderEvents();
    if (!events.length) {
      return [];
    }

    this.appointmentReminderEvents.set([]);
    return events;
  }

  private setConnectionState(state: ConnectionState): void {
    this.connectionState.set(state);
  }
}
