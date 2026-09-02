import { Component, OnInit, OnDestroy, ChangeDetectorRef, ViewChild, ElementRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';
import { ChatService, ChatMessage, Conversation } from '../../services/chat';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat.html',
  styleUrls: ['./chat.scss']
})
export class ChatComponent implements OnInit, OnDestroy {
  @ViewChild('scrollContainer') private scrollContainer!: ElementRef;

  public messages: ChatMessage[] = [];
  public conversations: Conversation[] = [];
  public selectedConversation: Conversation | null = null;
  public newMessageContent: string = '';

  public myId: string = 'user1';
  public currentUserId: string = 'user1';
  public receiverId: string = 'user2';

  public unreadCount: number = 0;

  private messageSub!: Subscription;
  private routeSub!: Subscription;
  private isInitialLoad: boolean = true;
  private previousMessagesCount: number = 0;
  private totalGlobalMessagesCount: number = 0;

  constructor(
    private chatService: ChatService,
    private route: ActivatedRoute,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private ngZone: NgZone
  ) { }

  ngOnInit(): void {
    this.chatService.startConnection();
    this.myId = this.chatService.getMyUserIdFromToken();
    this.currentUserId = this.myId;

    this.loadConversations();

    // 1. URL PRETPLATA - Menja aktivnog sagovornika ili reaguje na F5
    this.routeSub = this.route.queryParams.subscribe(params => {
      if (params['to']) {
        this.receiverId = params['to'];
      } else {
        this.receiverId = (this.myId.toLowerCase() === 'user1') ? 'user2' : 'user1';
      }

      this.isInitialLoad = true;
      this.previousMessagesCount = 0;
      this.unreadCount = 0;

      this.hideScrollContainer();
      this.chatService.loadHistory(this.receiverId, this.myId);
    });

    // 2. SIGNALR PRETPLATA - Reaguje na sve poruke
    this.messageSub = this.chatService.messages$.subscribe((allMessages) => {
      this.ngZone.run(() => {
        const myClean = String(this.myId).trim().toLowerCase();
        const recClean = String(this.receiverId).trim().toLowerCase();

        const isNewGlobalMessage = allMessages.length > this.totalGlobalMessagesCount;
        this.totalGlobalMessagesCount = allMessages.length;

        const filtered = allMessages.filter(m => {
          const s = String(m.senderId || '').trim().toLowerCase();
          const r = String(m.receiverId || '').trim().toLowerCase();
          return (s === myClean && r === recClean) || (s === recClean && r === myClean);
        });

        // 1. Inicijalno učitavanje pri otvaranju / osvežavanju (F5)
        if (this.isInitialLoad) {
          this.messages = filtered;
          this.previousMessagesCount = filtered.length;
          this.cdr.detectChanges();

          // Trenutni skok na dno BEZ animacije pre nego što se prikaže
          this.forceScrollBottomInstant();
          this.showScrollContainer();

          // Dodatna provera nakon što pretraživač završi render
          setTimeout(() => {
            this.forceScrollBottomInstant();
            this.isInitialLoad = false;
          }, 50);
          return;
        }

        // 2. Ako stignu NOVE poruke u toku razgovora
        const newMessagesDelta = filtered.length - this.previousMessagesCount;
        this.messages = filtered;

        const latestGlobalMsg = allMessages[allMessages.length - 1];

        if (latestGlobalMsg && isNewGlobalMessage) {
          const sender = String(latestGlobalMsg.senderId).trim().toLowerCase();
          const receiver = String(latestGlobalMsg.receiverId).trim().toLowerCase();

          const otherUser = (sender === myClean) ? receiver : sender;
          const conv = this.conversations.find(c => c.userId.toLowerCase() === otherUser);

          if (conv) {
            conv.lastMessage = latestGlobalMsg.content;
            conv.lastMessageTime = new Date().toISOString();

            if (sender !== myClean && sender !== recClean) {
              conv.hasUnread = true;
              conv.unreadCount = Number(conv.unreadCount || 0) + 1;
            }

            this.sortConversations();
          } else {
            this.loadConversations();
          }
        }

        if (newMessagesDelta > 0) {
          const lastMsg = this.messages[this.messages.length - 1];
          const isFromOther = String(lastMsg?.senderId).trim().toLowerCase() !== myClean;

          if (isFromOther) {
            if (this.isUserNearBottom()) {
              this.animatedScrollToBottom();
              this.chatService.markAsRead(this.receiverId).subscribe();
            } else {
              this.unreadCount += newMessagesDelta;
            }
          } else {
            this.animatedScrollToBottom();
          }
        }

        this.previousMessagesCount = filtered.length;
        this.cdr.detectChanges();
      });
    });
  }

  private sortConversations(): void {
    this.conversations.sort((a, b) => {
      const timeA = a.lastMessageTime ? new Date(a.lastMessageTime).getTime() : 0;
      const timeB = b.lastMessageTime ? new Date(b.lastMessageTime).getTime() : 0;
      return timeB - timeA;
    });
  }

  public loadConversations(): void {
    this.chatService.getConversations().subscribe({
      next: (data: any[]) => {
        this.conversations = (data || [])
          .filter(c => c && (c.userId || c.UserId))
          .map(c => {
            const count = c.unreadCount ?? c.UnreadCount ?? 0;
            return {
              ...c,
              userId: c.userId || c.UserId,
              unreadCount: Number(count),
              hasUnread: c.hasUnread ?? c.HasUnread ?? (count > 0)
            };
          });

        this.sortConversations();
        this.cdr.detectChanges();
      },
      error: (err) => console.error('❌ Greška pri učitavanju konverzacija:', err)
    });
  }

  public selectConversation(conv: Conversation): void {
    conv.hasUnread = false;
    conv.unreadCount = 0;

    this.selectedConversation = conv;

    if (this.receiverId !== conv.userId) {
      this.receiverId = conv.userId;

      this.router.navigate([], {
        relativeTo: this.route,
        queryParams: { to: conv.userId },
        queryParamsHandling: 'merge'
      });

      this.isInitialLoad = true;
      this.unreadCount = 0;
      this.previousMessagesCount = 0;

      this.hideScrollContainer();
      this.chatService.loadHistory(this.receiverId, this.myId);
    }

    this.chatService.markAsRead(conv.userId).subscribe({
      error: (err) => console.error('Greška pri označavanju poruka kao pročitanih:', err)
    });
  }

  sendMessage(): void {
    if (!this.newMessageContent.trim()) return;

    const sentContent = this.newMessageContent;
    this.chatService.sendMessage(this.receiverId, sentContent, this.myId);
    this.newMessageContent = '';

    this.unreadCount = 0;
    this.animatedScrollToBottom();

    const recClean = String(this.receiverId).trim().toLowerCase();
    const conv = this.conversations.find(c => c.userId.toLowerCase() === recClean);

    if (conv) {
      conv.lastMessage = sentContent;
      conv.lastMessageTime = new Date().toISOString();
      this.sortConversations();
    }
  }

  scrollToBottomManual(): void {
    this.unreadCount = 0;
    this.cdr.detectChanges();
    this.animatedScrollToBottom();
    this.chatService.markAsRead(this.receiverId).subscribe();
  }

  onScroll(): void {
    if (this.isUserNearBottom()) {
      if (this.unreadCount !== 0) {
        this.unreadCount = 0;
        this.cdr.detectChanges();
        this.chatService.markAsRead(this.receiverId).subscribe();
      }
    }
  }

  private isUserNearBottom(): boolean {
    if (!this.scrollContainer) return false;
    const threshold = 150;
    const position = this.scrollContainer.nativeElement.scrollTop + this.scrollContainer.nativeElement.clientHeight;
    const height = this.scrollContainer.nativeElement.scrollHeight;
    return height - position <= threshold;
  }

  // TRENUTNI SKOK BEZ ANIMACIJE
  private forceScrollBottomInstant(): void {
    if (this.scrollContainer) {
      const el = this.scrollContainer.nativeElement;
      el.style.scrollBehavior = 'auto';
      el.scrollTop = el.scrollHeight;
    }
  }

  // ANIMIRANO SKROLOVANJE SVE SVEŽE PORUKE
  private animatedScrollToBottom(): void {
    setTimeout(() => {
      if (this.scrollContainer) {
        const el = this.scrollContainer.nativeElement;
        el.style.scrollBehavior = 'smooth';
        el.scrollTop = el.scrollHeight;
      }
    }, 50);
  }

  private hideScrollContainer(): void {
    if (this.scrollContainer) {
      this.scrollContainer.nativeElement.style.visibility = 'hidden';
    }
  }

  private showScrollContainer(): void {
    if (this.scrollContainer) {
      this.scrollContainer.nativeElement.style.visibility = 'visible';
    }
  }

  ngOnDestroy(): void {
    if (this.messageSub) this.messageSub.unsubscribe();
    if (this.routeSub) this.routeSub.unsubscribe();
  }
}
