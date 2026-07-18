import { Component, ElementRef, HostListener, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { MarkdownComponent } from 'ngx-markdown';
import { AuthService } from '../../Services/auth-service';
import { NotificationService } from '../../Services/notification.service';
import { HealthProfileService } from '../../Services/health-profile.service';
import { AiChatService } from '../../Services/ai-chat.service';
import { LocalizationService } from '../../Services/localization.service';
import { ConversationMessageDto, ConversationSummaryDto } from '../../Modles/ai-chat.models';
import { catchError, of } from 'rxjs';

/** Local-only chat message shape: adds an optional client-side image preview
 * for the current session (the backend does not persist/return image bytes on
 * reload, so this is only shown for messages sent during this session). */
type ChatMessage = ConversationMessageDto & { imagePreviewUrl?: string };

const ACCEPTED_IMAGE_TYPES: Record<string, string> = {
  'image/jpeg': 'jpeg',
  'image/jpg': 'jpeg',
  'image/png': 'png',
  'image/webp': 'webp',
};

const MAX_IMAGE_SIZE_BYTES = 8 * 1024 * 1024; // 8 MB

@Component({
  selector: 'app-ai-assistant',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, MarkdownComponent],
  templateUrl: './ai-assistant.html',
  styleUrl: './ai-assistant.css',
})
export class AiAssistant implements OnInit {
  private readonly authService = inject(AuthService);
  protected readonly notifService = inject(NotificationService);
  private readonly healthProfileService = inject(HealthProfileService);
  private readonly aiChatService = inject(AiChatService);
  private readonly router = inject(Router);
  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;

  @ViewChild('messagesEnd') private messagesEnd?: ElementRef<HTMLDivElement>;
  @ViewChild('messageInput') private messageInputRef?: ElementRef<HTMLTextAreaElement>;
  @ViewChild('fileInput') private fileInputRef?: ElementRef<HTMLInputElement>;

  // ── Sidebar / header (shared shell state, same as other pages) ──
  readonly sidebarCollapsed = signal(false);
  readonly mobileSidebarOpen = signal(false);
  readonly dropdownOpen = signal(false);
  readonly unreadNotifCount = this.notifService.unreadCount;
  readonly rightPanelCollapsed = signal(true);

  // ── Profile ──
  readonly profileId = signal<string | null>(null);
  readonly profileLoading = signal(true);
  readonly profileError = signal(false);

  // ── Conversations ──
  readonly conversations = signal<ConversationSummaryDto[]>([]);
  readonly conversationsLoading = signal(true);
  readonly selectedConversationId = signal<string | null>(null);
  readonly searchTerm = signal('');
  readonly showAllConversations = signal(false);

  readonly filteredConversations = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const list = this.conversations();
    if (!term) return list;
    return list.filter(c => c.title.toLowerCase().includes(term));
  });

  readonly visibleConversations = computed(() => {
    const list = this.filteredConversations();
    return this.showAllConversations() ? list : list.slice(0, 6);
  });

  readonly selectedConversation = computed(() =>
    this.conversations().find(c => c.id === this.selectedConversationId()) ?? null
  );

  // ── Messages ──
  readonly messages = signal<ChatMessage[]>([]);
  readonly messagesLoading = signal(false);
  readonly messageText = signal('');
  readonly sending = signal(false);
  readonly sendError = signal<string | null>(null);

  // ── Image attachment ──
  readonly attachedImagePreviewUrl = signal<string | null>(null);
  readonly attachedImageBase64 = signal<string | null>(null);
  readonly attachedImageFormat = signal<string | null>(null);
  readonly attachError = signal<string | null>(null);

  // ── Rename ──
  readonly renaming = signal(false);
  readonly renameDraft = signal('');

  // ── Dislike reason dialog ──
  readonly dislikeDialogOpen = signal(false);
  readonly dislikeTargetMsg = signal<ChatMessage | null>(null);
  readonly dislikeReason = signal('');
  readonly dislikeReasons = [
    'Incorrect information',
    'Harmful or unsafe content',
    'Not helpful',
    'Off-topic',
    'Other',
  ];
  readonly dislikeSubmitting = signal(false);

  get displayName(): string {
    const u = this.authService.currentUser;
    if (!u) return 'there';
    return u.firstName?.trim() || u.email;
  }

  get userEmail(): string {
    return this.authService.currentUser?.email ?? '';
  }

  get avatarUrl(): string {
    return this.authService.avatarUrl;
  }

  ngOnInit(): void {
    this.applyResponsiveSidebar();
    this.loadProfileThenConversations();
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

  private loadConversations(): void {
    this.conversationsLoading.set(true);
    this.aiChatService.getConversations().subscribe(list => {
      this.conversations.set(list);
      this.conversationsLoading.set(false);
    });
  }

  // ── Conversation list actions ──

  startNewConversation(): void {
    this.selectedConversationId.set(null);
    this.messages.set([]);
    this.sendError.set(null);
    this.messageText.set('');
    this.clearAttachedImage();
    this.focusInput();
  }

  selectConversation(conversation: ConversationSummaryDto): void {
    if (this.selectedConversationId() === conversation.id) return;

    this.selectedConversationId.set(conversation.id);
    this.sendError.set(null);
    this.renaming.set(false);
    this.messagesLoading.set(true);
    this.messages.set([]);
    this.clearAttachedImage();

    this.aiChatService.getConversationHistory(conversation.id).subscribe(history => {
      this.messages.set(history?.messages ?? []);
      this.messagesLoading.set(false);
      this.scrollToBottom();
    });
  }

  toggleShowAllConversations(): void {
    this.showAllConversations.update(v => !v);
  }

  // ── Rename ──

  startRename(): void {
    const conv = this.selectedConversation();
    if (!conv) return;
    this.renameDraft.set(conv.title);
    this.renaming.set(true);
  }

  cancelRename(): void {
    this.renaming.set(false);
  }

  confirmRename(): void {
    const conv = this.selectedConversation();
    const title = this.renameDraft().trim();
    if (!conv || !title) {
      this.renaming.set(false);
      return;
    }

    this.aiChatService.renameConversation(conv.id, title).subscribe({
      next: res => {
        const updated = res.data;
        if (updated) {
          this.conversations.update(list => list.map(c => (c.id === conv.id ? { ...c, title: updated.title } : c)));
        }
        this.renaming.set(false);
      },
      error: () => this.renaming.set(false),
    });
  }

  // ── Archive ──

  archiveConversation(): void {
    const conv = this.selectedConversation();
    if (!conv) return;
    if (!confirm('Archive this conversation? It will be removed from your conversation list.')) return;

    this.aiChatService.archiveConversation(conv.id).subscribe(() => {
      this.conversations.update(list => list.filter(c => c.id !== conv.id));
      if (this.selectedConversationId() === conv.id) {
        this.selectedConversationId.set(null);
        this.messages.set([]);
      }
    });
  }

  // ── Image attachment ──

  triggerFileSelect(): void {
    if (this.sending()) return;
    this.fileInputRef?.nativeElement.click();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    input.value = ''; // allow re-selecting the same file later

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

  // ── Sending ──

  onMessageInput(value: string): void {
    this.messageText.set(value);
  }

  sendMessage(): void {
    const text = this.messageText().trim();
    const imageBase64 = this.attachedImageBase64();
    const imageFormat = this.attachedImageFormat();

    // Allow sending an image with no caption, but require at least one of the two.
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

    // No conversation yet — create one titled from the first message, then send into it.
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
          const content = res.data?.content ?? '';
          const nextSeq = (this.messages().at(-1)?.sequenceNumber ?? 0) + 1;
          this.messages.update(list => [
            ...list,
            {
              id: `assistant-${nextSeq}`,
              role: 'Assistant',
              content,
              sequenceNumber: nextSeq,
              createdAt: new Date().toISOString(),
            },
          ]);
          this.conversations.update(list =>
            list
              .map(c => (c.id === conversationId ? { ...c, lastMessageAt: new Date().toISOString() } : c))
              .sort((a, b) => this.sortKey(b) - this.sortKey(a))
          );
          this.sending.set(false);
          this.scrollToBottom();
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
    setTimeout(() => this.messageInputRef?.nativeElement.focus(), 0);
  }

  private scrollToBottom(): void {
    setTimeout(() => this.messagesEnd?.nativeElement.scrollIntoView({ behavior: 'smooth' }), 0);
  }

  // ── Formatting helpers ──

  formatSidebarTime(conv: ConversationSummaryDto): string {
    const dateStr = conv.lastMessageAt ?? conv.createdAt;
    const date = new Date(dateStr);
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

  formatMessageTime(message: ConversationMessageDto): string {
    return new Date(message.createdAt).toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
  }

  formatHeaderLastUpdated(): string {
    const conv = this.selectedConversation();
    if (!conv) return '';
    const dateStr = conv.lastMessageAt ?? conv.createdAt;
    return this.formatSidebarTime(conv) + (new Date(dateStr).toDateString() === new Date().toDateString() ? ', ' + this.t().aiAssistant.today : '');
  }

  isUserMessage(message: ConversationMessageDto): boolean {
    return message.role === 'User';
  }

  toggleReaction(msg: ChatMessage, type: 'ThumbsUp' | 'ThumbsDown'): void {
    // Don't allow reactions on optimistic user messages that have no real ID yet
    if (msg.id.startsWith('pending-')) return;

    if (type === 'ThumbsDown' && msg.userReaction !== 'ThumbsDown') {
      // Open dislike reason dialog first
      this.dislikeTargetMsg.set(msg);
      this.dislikeReason.set('');
      this.dislikeDialogOpen.set(true);
      return;
    }

    this.applyReaction(msg, type);
  }

  applyReaction(msg: ChatMessage, type: 'ThumbsUp' | 'ThumbsDown'): void {
    const conversationId = this.selectedConversationId();
    if (!conversationId) return;

    const remove = msg.userReaction === type;
    const previousReaction = msg.userReaction;

    // Optimistically update UI
    this.messages.update(list =>
      list.map(m => m.id === msg.id ? { ...m, userReaction: remove ? null : type } : m)
    );

    this.aiChatService.reactToMessage(conversationId, msg.id, type, remove).subscribe({
      error: () => {
        // Rollback on error
        this.messages.update(list =>
          list.map(m => m.id === msg.id ? { ...m, userReaction: previousReaction } : m)
        );
      }
    });
  }

  submitDislikeReason(): void {
    const msg = this.dislikeTargetMsg();
    if (!msg) return;

    this.dislikeDialogOpen.set(false);
    this.applyReaction(msg, 'ThumbsDown');
    this.dislikeTargetMsg.set(null);
    this.dislikeReason.set('');
  }

  cancelDislikeDialog(): void {
    this.dislikeDialogOpen.set(false);
    this.dislikeTargetMsg.set(null);
    this.dislikeReason.set('');
  }

  // ── Shell chrome (mirrors dashboard.ts) ──

  @HostListener('window:resize')
  onWindowResize(): void {
    this.applyResponsiveSidebar();
  }

  private applyResponsiveSidebar(): void {
    this.sidebarCollapsed.set(window.innerWidth <= 1024);
    if (window.innerWidth > 768) {
      this.mobileSidebarOpen.set(false);
    }
  }

  toggleSidebar(): void {
    this.sidebarCollapsed.update(v => !v);
  }

  toggleMobileSidebar(): void {
    this.mobileSidebarOpen.update(v => !v);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.hdr-user')) {
      this.dropdownOpen.set(false);
    }
  }

  toggleDropdown(): void {
    this.dropdownOpen.update(v => !v);
  }

  logout(): void {
    this.dropdownOpen.set(false);
    this.authService.logout().subscribe();
  }
}
