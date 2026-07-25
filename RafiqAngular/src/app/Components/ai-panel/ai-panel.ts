import { Component, ElementRef, HostListener, OnDestroy, OnInit, ViewChild, computed, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, NavigationStart } from '@angular/router';
import { MarkdownComponent } from 'ngx-markdown';
import { AuthService } from '../../Services/auth-service';
import { HealthProfileService } from '../../Services/health-profile.service';
import { AiChatService } from '../../Services/ai-chat.service';
import { LocalizationService } from '../../Services/localization.service';
import { ReviewTrackingService } from '../../Services/review-tracking.service';
import { ConversationMessageDto, ConversationSummaryDto } from '../../Modles/ai-chat.models';
import { catchError, of, Subscription } from 'rxjs';
import { VoiceAgentPanel } from '../voice-agent-panel/voice-agent-panel';

type ChatMessage = ConversationMessageDto & { imagePreviewUrl?: string };

const ACCEPTED_IMAGE_TYPES: Record<string, string> = {
  'image/jpeg': 'jpeg',
  'image/jpg': 'jpeg',
  'image/png': 'png',
  'image/webp': 'webp',
};
const MAX_IMAGE_SIZE_BYTES = 8 * 1024 * 1024; // 8 MB

@Component({
  selector: 'app-ai-panel',
  standalone: true,
  imports: [CommonModule, MarkdownComponent, VoiceAgentPanel],
  templateUrl: './ai-panel.html',
  styleUrl: './ai-panel.css',
})
export class AiPanel implements OnInit, OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly healthProfileService = inject(HealthProfileService);
  private readonly router = inject(Router);
  protected readonly aiChatService = inject(AiChatService);
  protected readonly l10n = inject(LocalizationService);
  private readonly reviewTracking = inject(ReviewTrackingService);
  protected readonly t = this.l10n.t;

  private readonly _routerSub: Subscription;

  @ViewChild('messagesEnd') private messagesEnd?: ElementRef<HTMLDivElement>;
  @ViewChild('messageInput') private messageInputRef?: ElementRef<HTMLTextAreaElement>;
  @ViewChild('fileInput') private fileInputRef?: ElementRef<HTMLInputElement>;

  // ── Global State ──
  readonly isPanelOpen = this.aiChatService.isPanelOpen;

  // ── Mode: chat or voice ──
  readonly activeMode = signal<'chat' | 'voice'>('chat');

  toggleMode(): void {
    this.activeMode.update(m => m === 'chat' ? 'voice' : 'chat');
  }

  // ── Sidebar collapse — hidden by default ──
  readonly sidebarCollapsed = signal(true);

  // ── Window state ──
  readonly minimized          = signal(false);
  readonly maximized          = signal(false);
  readonly isDragging         = signal(false);
  readonly dragPos            = signal<{ top: number; left: number } | null>(null);
  // True after the first user interaction — prevents the open animation from
  // re-running when minimize/maximize/drag resets other style bindings.
  readonly _suppressAnimation = signal(false);

  // Internal drag tracking
  private _drag = {
    pending: false,   // mousedown recorded but threshold not yet met
    active:  false,   // actually dragging
    startX: 0, startY: 0,
    startLeft: 0, startTop: 0,
    panelW: 0, panelH: 0,
  };

  toggleSidebar(): void {
    this.sidebarCollapsed.update(v => !v);
  }

  // ── Profile ──
  readonly profileId = signal<string | null>(null);
  readonly profileLoading = signal(true);
  readonly profileError = signal(false);

  // ── Conversations ──
  readonly conversations = signal<ConversationSummaryDto[]>([]);
  readonly conversationsLoading = signal(true);
  readonly selectedConversationId = signal<string | null>(null);

  readonly selectedConversation = computed(() =>
    this.conversations().find(c => c.id === this.selectedConversationId()) ?? null
  );

  // ── Pin (persisted to localStorage) ──
  private readonly PINS_KEY = 'rafiq_pinned_convs';
  readonly pinnedIds = signal<Set<string>>(this.loadPins());

  readonly sortedConversations = computed(() => {
    const pinned = this.pinnedIds();
    return [...this.conversations()].sort((a, b) => {
      const aPin = pinned.has(a.id) ? 1 : 0;
      const bPin = pinned.has(b.id) ? 1 : 0;
      if (bPin !== aPin) return bPin - aPin;
      return this.sortKey(b) - this.sortKey(a);
    });
  });

  private loadPins(): Set<string> {
    try { return new Set(JSON.parse(localStorage.getItem(this.PINS_KEY) ?? '[]')); }
    catch { return new Set(); }
  }
  private savePins(): void {
    localStorage.setItem(this.PINS_KEY, JSON.stringify([...this.pinnedIds()]));
  }

  togglePin(event: Event, id: string): void {
    event.stopPropagation();
    this.pinnedIds.update(s => {
      const next = new Set(s);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
    this.savePins();
  }

  // ── Inline rename ──
  readonly renamingId   = signal<string | null>(null);
  readonly renameValue  = signal('');

  startRename(convId: string, title: string): void {
    this.openMenuId.set(null);
    this.renamingId.set(convId);
    this.renameValue.set(title);
  }

  commitRename(convId: string): void {
    const title = this.renameValue().trim();
    this.renamingId.set(null);
    if (!title) return;
    this.conversations.update(list =>
      list.map(c => c.id === convId ? { ...c, title } : c)
    );
    this.aiChatService.renameConversation(convId, title).subscribe({
      error: () => this.loadConversations()
    });
  }

  cancelRename(): void { this.renamingId.set(null); }

  // ── Context menu (⋮) ──
  readonly openMenuId = signal<string | null>(null);

  toggleMenu(event: Event, id: string): void {
    event.stopPropagation();
    this.openMenuId.update(cur => cur === id ? null : id);
  }

  closeMenu(): void { this.openMenuId.set(null); }

  // ── Messages ──
  readonly messages = signal<ChatMessage[]>([]);
  readonly messagesLoading = signal(false);
  readonly messageText = signal('');
  readonly sending = signal(false);
  readonly sendError = signal<string | null>(null);

  // ── Dislike reason dialog ──
  readonly dislikeDialogOpen = signal(false);
  readonly dislikeTargetMsg = signal<ChatMessage | null>(null);
  readonly dislikeReason = signal('');
  readonly dislikeFreeText = signal('');
  readonly dislikeReasons = computed(() => this.t().aiAssistant.feedbackReasons);
  readonly dislikeSubmitting = signal(false);

  // ── Image attachment ──
  readonly attachedImagePreviewUrl = signal<string | null>(null);
  readonly attachedImageBase64 = signal<string | null>(null);
  readonly attachedImageFormat = signal<string | null>(null);
  readonly attachError = signal<string | null>(null);

  get avatarUrl(): string {
    return this.authService.avatarUrl;
  }

  get displayName(): string {
    const u = this.authService.currentUser;
    return u?.firstName?.trim() || u?.email || 'there';
  }

  readonly suggestedPrompts = computed(() => this.t().aiAssistant.suggestedPrompts);

  private static readonly PUBLIC_ROUTES = ['/', '/login', '/register', '/forgot-password', '/verify-account'];

  constructor() {
    this._routerSub = this.router.events.subscribe(e => {
      if (e instanceof NavigationStart && AiPanel.PUBLIC_ROUTES.some(r => e.url === r || e.url.startsWith(r + '?'))) {
        this.aiChatService.closePanel();
      }
    });

    effect(() => {
      if (this.isPanelOpen()) {
        if (!this.profileId() && !this.profileError()) {
          this.loadProfileThenConversations();
        } else {
          this.scrollToBottom();
        }
      }
    });

    // When the robot is clicked from the sidebar, switch to voice mode.
    effect(() => {
      const req = this.aiChatService.voiceModeRequest();
      if (req > 0) {
        this.activeMode.set('voice');
      }
    });
  }

  ngOnInit(): void {}

  ngOnDestroy(): void {
    this._routerSub.unsubscribe();
  }

  closePanel(): void {
    this.minimized.set(false);
    this.maximized.set(false);
    this.dragPos.set(null);
    this._drag.active = false;
    this._drag.pending = false;
    this._suppressAnimation.set(false); // allow slide-in on next open
    this.aiChatService.closePanel();
  }

  toggleMinimize(): void {
    this._suppressAnimation.set(true);
    if (this.minimized()) {
      this.dragPos.set(null);
      this._drag.active = false;
      this._drag.pending = false;
    }
    this.maximized.set(false);
    this.minimized.update(v => !v);
  }

  toggleMaximize(): void {
    this._suppressAnimation.set(true);
    this.minimized.set(false);
    this.dragPos.set(null);
    this._drag.active = false;
    this._drag.pending = false;
    this.maximized.update(v => !v);
  }

  // ── Drag handlers ──
  // Buttons/anchors inside the header are excluded from drag.
  // A 5px movement threshold distinguishes drag from a click.
  onHeaderPointerDown(event: MouseEvent | TouchEvent): void {
    if ((event.target as Element).closest('button, a')) return;
    const clientX = event instanceof TouchEvent ? event.touches[0].clientX : (event as MouseEvent).clientX;
    const clientY = event instanceof TouchEvent ? event.touches[0].clientY : (event as MouseEvent).clientY;
    const panel = (event.currentTarget as Element).closest('.ai-panel') as HTMLElement;
    if (!panel) return;
    const rect = panel.getBoundingClientRect();
    this._drag = {
      pending: true,
      active: false,
      startX: clientX, startY: clientY,
      startLeft: rect.left, startTop: rect.top,
      panelW: rect.width, panelH: rect.height,
    };
  }

  @HostListener('document:mousemove', ['$event'])
  @HostListener('document:touchmove', ['$event'])
  onPointerMove(event: MouseEvent | TouchEvent): void {
    if (!this._drag.pending && !this._drag.active) return;
    const clientX = event instanceof TouchEvent ? event.touches[0].clientX : (event as MouseEvent).clientX;
    const clientY = event instanceof TouchEvent ? event.touches[0].clientY : (event as MouseEvent).clientY;
    const dx = clientX - this._drag.startX;
    const dy = clientY - this._drag.startY;

    if (this._drag.pending && Math.sqrt(dx * dx + dy * dy) >= 5) {
      this._drag.pending = false;
      this._drag.active = true;
      this._suppressAnimation.set(true);
      this.isDragging.set(true);
    }

    if (!this._drag.active) return;
    const top  = Math.max(0, Math.min(window.innerHeight - this._drag.panelH, this._drag.startTop  + dy));
    const left = Math.max(0, Math.min(window.innerWidth  - this._drag.panelW, this._drag.startLeft + dx));
    this.dragPos.set({ top, left });
    event.preventDefault();
  }

  @HostListener('document:mouseup')
  @HostListener('document:touchend')
  onPointerUp(): void {
    this._drag.pending = false;
    if (!this._drag.active) return;
    this._drag.active = false;
    this.isDragging.set(false);
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    if (this.openMenuId()) this.openMenuId.set(null);
  }


  private loadProfileThenConversations(): void {
    this.healthProfileService
      .getMyProfile()
      .pipe(catchError(() => of(null)))
      .subscribe(res => {
        const id = res?.data?.id ?? null;
        this.profileId.set(id);
        this.profileLoading.set(false);
        this.profileError.set(!id);

        if (id) {
          this.loadConversations();
        } else {
          this.conversationsLoading.set(false);
        }
      });
  }

  loadConversations(): void {
    this.conversationsLoading.set(true);
    this.aiChatService.getConversations().subscribe(list => {
      this.conversations.set(list);
      this.conversationsLoading.set(false);
    });
  }

  startNewConversation(): void {
    this.selectedConversationId.set(null);
    this.messages.set([]);
    this.sendError.set(null);
    this.messageText.set('');
    this.clearAttachedImage();
    this.focusInput();
  }

  usePrompt(prompt: string): void {
    this.startNewConversation();
    this.messageText.set(prompt);
    this.sendMessage();
  }

  deleteConversation(event: Event, conversationId: string): void {
    event.stopPropagation();
    // Optimistic removal
    this.conversations.update(list => list.filter(c => c.id !== conversationId));
    if (this.selectedConversationId() === conversationId) {
      this.startNewConversation();
    }
    this.aiChatService.archiveConversation(conversationId).subscribe({
      error: () => {
        // Restore on failure
        this.loadConversations();
      }
    });
  }

  selectConversation(conversation: ConversationSummaryDto): void {
    if (this.selectedConversationId() === conversation.id) {
      return;
    }

    this.selectedConversationId.set(conversation.id);
    this.sendError.set(null);
    this.messagesLoading.set(true);
    this.messages.set([]);
    this.clearAttachedImage();

    this.aiChatService.getConversationHistory(conversation.id).subscribe(history => {
      const convId = conversation.id;
      this.messages.set(
        (history?.messages ?? []).map(m => ({
          ...m,
          imagePreviewUrl: m.role === 'User'
            ? this.aiChatService.getCachedImage(convId, m.sequenceNumber)
            : undefined,
        }))
      );
      this.messagesLoading.set(false);
      this.scrollToBottom();
    });
  }

  // ── Voice panel event handlers ──────────────────────────────────────────────

  onVoiceConversationCreated(summary: ConversationSummaryDto): void {
    this.conversations.update(list => [summary, ...list]);
    this.selectedConversationId.set(summary.id);
  }

  // Called when the voice panel captures speech — mirrors pushOptimisticUserMessage
  // + sending indicator so the shared message list shows the same experience as typing.
  onVoiceMessageStarted(text: string): void {
    this.pushOptimisticUserMessage(text);
    this.sending.set(true);
  }

  onVoiceResponseReceived(conversationId: string): void {
    this.aiChatService.getConversationHistory(conversationId).subscribe(history => {
      this.messages.set(
        (history?.messages ?? []).map(m => ({
          ...m,
          imagePreviewUrl: m.role === 'User'
            ? this.aiChatService.getCachedImage(conversationId, m.sequenceNumber)
            : undefined,
        }))
      );
      this.conversations.update(list =>
        list
          .map(c => c.id === conversationId ? { ...c, lastMessageAt: new Date().toISOString() } : c)
          .sort((a, b) => this.sortKey(b) - this.sortKey(a))
      );
      this.sending.set(false);
      this.scrollToBottom();
    });
  }

  triggerFileSelect(): void {
    if (this.sending()) return;
    this.fileInputRef?.nativeElement.click();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    input.value = ''; 

    if (!file) return;

    this.attachError.set(null);
    const format = ACCEPTED_IMAGE_TYPES[file.type];
    if (!format) {
      this.attachError.set(this.t().aiAssistant.unsupportedFormat);
      return;
    }
    if (file.size > MAX_IMAGE_SIZE_BYTES) {
      this.attachError.set(this.t().aiAssistant.imageTooLarge);
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      const result = reader.result as string;
      const base64 = result.replace(/^data:.*;base64,/, '');
      this.attachedImageBase64.set(base64);
      this.attachedImageFormat.set(format);
      this.attachedImagePreviewUrl.set(result);
    };
    reader.onerror = () => {
      this.attachError.set(this.t().aiAssistant.couldNotReadImage);
    };
    reader.readAsDataURL(file);
  }

  removeAttachedImage(): void {
    this.clearAttachedImage();
  }

  private clearAttachedImage(): void {
    this.attachedImagePreviewUrl.set(null);
    this.attachedImageBase64.set(null);
    this.attachedImageFormat.set(null);
    this.attachError.set(null);
  }

  onMessageInput(value: string): void {
    this.messageText.set(value);
  }

  sendMessage(): void {
    const text = this.messageText().trim();
    const imageBase64 = this.attachedImageBase64();
    const imageFormat = this.attachedImageFormat();

    if ((!text && !imageBase64) || this.sending()) return;

    const profileId = this.profileId();
    if (!profileId) {
      this.sendError.set(this.t().aiAssistant.noProfileError);
      return;
    }

    this.sendError.set(null);
    this.sending.set(true);
    this.messageText.set('');

    const imagePreviewUrl = this.attachedImagePreviewUrl() ?? undefined;
    this.clearAttachedImage();

    const existingId = this.selectedConversationId();

    if (existingId) {
      this.pushOptimisticUserMessage(text, imagePreviewUrl);
      this.doSendMessage(existingId, text, imageBase64, imageFormat);
      return;
    }

    const titleSource = text || 'Attached image';
    const title = titleSource.length > 40 ? `${titleSource.slice(0, 40)}…` : titleSource;
    this.aiChatService.createConversation({ userHealthProfileId: profileId, title }).subscribe({
      next: res => {
        const newId = res.data;
        if (!newId) {
          this.sending.set(false);
          this.sendError.set(this.t().aiAssistant.conversationFailed);
          this.messageText.set(text);
          return;
        }

        const summary: ConversationSummaryDto = {
          id: newId,
          userHealthProfileId: profileId,
          title,
          lastMessageAt: null,
          createdAt: new Date().toISOString(),
        };
        this.conversations.update(list => [summary, ...list]);
        this.selectedConversationId.set(newId);

        this.pushOptimisticUserMessage(text, imagePreviewUrl);
        this.doSendMessage(newId, text, imageBase64, imageFormat);
      },
      error: () => {
        this.sending.set(false);
        this.sendError.set(this.t().aiAssistant.conversationFailed);
        this.messageText.set(text);
      },
    });
  }

  private pushOptimisticUserMessage(text: string, imagePreviewUrl?: string): void {
    const nextSeq = (this.messages().at(-1)?.sequenceNumber ?? 0) + 1;
    const convId = this.selectedConversationId();
    if (imagePreviewUrl && convId) {
      this.aiChatService.cacheImage(convId, nextSeq, imagePreviewUrl);
    }
    this.messages.update(list => [
      ...list,
      {
        id: `pending-${nextSeq}`,
        role: 'User',
        content: text,
        sequenceNumber: nextSeq,
        createdAt: new Date().toISOString(),
        imagePreviewUrl,
      },
    ]);
    this.scrollToBottom();
  }

  private doSendMessage(
    conversationId: string,
    text: string,
    base64Image: string | null,
    imageFormat: string | null
  ): void {
    this.aiChatService
      .sendMessage(conversationId, { text, base64Image, imageFormat })
      .subscribe({
        next: res => {
          const id = res.data?.id ?? crypto.randomUUID();
          const content = res.data?.content ?? '';
          const nextSeq = (this.messages().at(-1)?.sequenceNumber ?? 0) + 1;
          this.messages.update(list => [
            ...list,
            {
              id,
              role: 'Assistant',
              content,
              sequenceNumber: nextSeq,
              createdAt: new Date().toISOString(),
            },
          ]);
          const isFirstExchange = this.messages().filter(m => m.role === 'Assistant').length === 1;
          this.conversations.update(list =>
            list
              .map(c => (c.id === conversationId ? { ...c, lastMessageAt: new Date().toISOString() } : c))
              .sort((a, b) => this.sortKey(b) - this.sortKey(a))
          );
          if (isFirstExchange) {
            this.aiChatService.generateConversationTitle(conversationId).subscribe({
              next: res => {
                if (res.data) {
                  this.conversations.update(list =>
                    list.map(c => c.id === conversationId ? { ...c, title: res.data! } : c)
                  );
                }
              },
            });
          }
          this.sending.set(false);
          this.scrollToBottom();
          this.reviewTracking.trackAction();
        },
        error: () => {
          this.sending.set(false);
          this.sendError.set(this.t().aiAssistant.sendFailed);
          this.messages.update(list => list.filter(m => !m.id.startsWith('pending-')));
          this.messageText.set(text);
        },
      });
  }

  private sortKey(c: ConversationSummaryDto): number {
    return new Date(c.lastMessageAt ?? c.createdAt).getTime();
  }

  onComposerKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  private focusInput(): void {
    setTimeout(() => this.messageInputRef?.nativeElement.focus(), 50);
  }

  private scrollToBottom(): void {
    setTimeout(() => this.messagesEnd?.nativeElement.scrollIntoView({ behavior: 'smooth' }), 50);
  }

  private asUtc(dateStr: string): Date {
    // EF Core returns DateTime without timezone suffix; treat bare ISO strings as UTC.
    if (dateStr && !dateStr.endsWith('Z') && !/[+-]\d{2}:\d{2}$/.test(dateStr)) {
      return new Date(dateStr + 'Z');
    }
    return new Date(dateStr);
  }

  formatMessageTime(message: ConversationMessageDto): string {
    return this.asUtc(message.createdAt).toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
  }

  formatSidebarTime(conv: ConversationSummaryDto): string {
    const dateStr = conv.lastMessageAt ?? conv.createdAt;
    const date = this.asUtc(dateStr);
    const now = new Date();
    const isToday = date.toDateString() === now.toDateString();

    const yesterday = new Date(now);
    yesterday.setDate(now.getDate() - 1);
    const isYesterday = date.toDateString() === yesterday.toDateString();

    if (isToday) {
      return date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true });
    }
    if (isYesterday) {
      return this.t().aiAssistant.yesterday;
    }
    const daysDiff = Math.floor((now.getTime() - date.getTime()) / 86_400_000);
    if (daysDiff < 7) {
      return date.toLocaleDateString('en-US', { weekday: 'long' });
    }
    return date.toLocaleDateString('en-GB');
  }

  isUserMessage(message: ConversationMessageDto): boolean {
    return message.role === 'User';
  }

  toggleReaction(msg: ChatMessage, type: 'ThumbsUp' | 'ThumbsDown'): void {
    if (msg.id.startsWith('pending-')) return;

    if (type === 'ThumbsDown' && msg.userReaction !== 'ThumbsDown') {
      this.dislikeTargetMsg.set(msg);
      this.dislikeReason.set('');
      this.dislikeDialogOpen.set(true);
      return;
    }

    this.applyReaction(msg, type);
  }

  applyReaction(msg: ChatMessage, type: 'ThumbsUp' | 'ThumbsDown', feedback?: string): void {
    const conversationId = this.selectedConversationId();
    if (!conversationId) return;

    const remove = msg.userReaction === type;
    const previousReaction = msg.userReaction;

    // Optimistic update
    this.messages.update(list =>
      list.map(m => m.id === msg.id ? { ...m, userReaction: remove ? null : type } : m)
    );

    this.aiChatService.reactToMessage(conversationId, msg.id, type, remove, feedback).subscribe({
      error: () => {
        // Revert on failure
        this.messages.update(list =>
          list.map(m => m.id === msg.id ? { ...m, userReaction: previousReaction } : m)
        );
      }
    });
  }

  submitDislikeReason(): void {
    const msg = this.dislikeTargetMsg();
    if (!msg) return;

    const selectedReason = this.dislikeReason();
    const freeText = this.dislikeFreeText().trim();
    const feedback = selectedReason
      ? (freeText ? `${selectedReason} — ${freeText}` : selectedReason)
      : (freeText || undefined);

    this.dislikeDialogOpen.set(false);
    this.applyReaction(msg, 'ThumbsDown', feedback);
    this.dislikeTargetMsg.set(null);
    this.dislikeReason.set('');
    this.dislikeFreeText.set('');
  }

  cancelDislikeDialog(): void {
    this.dislikeDialogOpen.set(false);
    this.dislikeTargetMsg.set(null);
    this.dislikeReason.set('');
    this.dislikeFreeText.set('');
  }
}
