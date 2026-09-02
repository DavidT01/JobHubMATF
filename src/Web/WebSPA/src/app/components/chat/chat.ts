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

    // 1. URL PRETPLATA - Menja aktivnog sagovornika
    this.routeSub = this.route.queryParams.subscribe(params => {
      if (params['to']) {
        this.receiverId = params['to'];
      } else {
        this.receiverId = (this.myId.toLowerCase() === 'user1') ? 'user2' : 'user1';
      }

      this.isInitialLoad = true;
      this.previousMessagesCount = 0;

      this.chatService.loadHistory(this.receiverId, this.myId);
    });

    // 2. SIGNALR PRETPLATA - Reaguje na sve nove poruke
    this.messageSub = this.chatService.messages$.subscribe((allMessages) => {
      this.ngZone.run(() => {
        const myClean = String(this.myId).trim().toLowerCase();
        const recClean = String(this.receiverId).trim().toLowerCase();

        // Provera da li je stigla bilo koja nova poruka na nivou aplikacije
        const isNewGlobalMessage = allMessages.length > this.totalGlobalMessagesCount;
        this.totalGlobalMessagesCount = allMessages.length;

        // Filtriramo poruke samo za trenutno otvoren chat u desnom prozoru
        const filtered = allMessages.filter(m => {
          const s = String(m.senderId || '').trim().toLowerCase();
          const r = String(m.receiverId || '').trim().toLowerCase();
          return (s === myClean && r === recClean) || (s === recClean && r === myClean);
        });

        const isNewMessageInCurrentChat = filtered.length > this.previousMessagesCount;
        this.messages = filtered;

        // Ažuriranje sidebara ako je stigla nova globalna poruka
        const latestGlobalMsg = allMessages[allMessages.length - 1];

        if (latestGlobalMsg && isNewGlobalMessage) {
          const sender = String(latestGlobalMsg.senderId).trim().toLowerCase();
          const receiver = String(latestGlobalMsg.receiverId).trim().toLowerCase();

          // Pronađi koja je to konverzacija
          const otherUser = (sender === myClean) ? receiver : sender;
          const conv = this.conversations.find(c => c.userId.toLowerCase() === otherUser);

          if (conv) {
            conv.lastMessage = latestGlobalMsg.content;
            conv.lastMessageTime = new Date().toISOString();

            // Povećaj bedž samo ako nam šalje neko drugi I nismo u chatu sa njim
            if (sender !== myClean && sender !== recClean) {
              conv.unreadCount = Number(conv.unreadCount || 0) + 1;
            }

            // Sortiraj sidebar po najsvežijoj poruci
            this.sortConversations();
          } else {
            // Ako je skroz nov sagovornik
            this.loadConversations();
          }
        }

        // Desni plutajući bedž (samo za poruke u otvorenim chatu)
        setTimeout(() => {
          if (this.isInitialLoad) {
            this.smartScrollToBottom(true);
            this.isInitialLoad = false;
          } else if (isNewMessageInCurrentChat) {
            const lastMsg = this.messages[this.messages.length - 1];
            const isFromOther = String(lastMsg?.senderId).trim().toLowerCase() !== myClean;

            if (isFromOther) {
              if (this.isUserNearBottom()) {
                this.smartScrollToBottom(false);
              } else {
                this.unreadCount++;
              }
            } else {
              this.smartScrollToBottom(true);
            }
          }
          this.previousMessagesCount = this.messages.length;
          this.cdr.detectChanges();
        }, 0);
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
      next: (data) => {
        this.conversations = (data || [])
          .filter(c => c && c.userId && c.userId.trim() !== '')
          .map(c => ({
            ...c,
            unreadCount: c.unreadCount ?? 0
          }));

        this.sortConversations();

        if (this.conversations.length > 0) {
          const found = this.conversations.find(c => c.userId.toLowerCase() === this.receiverId.toLowerCase());
          if (found) {
            this.selectedConversation = found;
          } else if (!this.receiverId || this.receiverId === 'user2') {
            this.selectedConversation = this.conversations[0];
            this.receiverId = this.conversations[0].userId;
          }
        }

        this.cdr.detectChanges();
      },
      error: (err) => console.error('❌ Greška pri učitavanju konverzacija:', err)
    });
  }

  public selectConversation(conv: Conversation): void {
    if (this.receiverId === conv.userId) return;

    // Resetuj bedž SAMO za kliknutu konverzaciju
    conv.unreadCount = 0;

    this.selectedConversation = conv;
    this.receiverId = conv.userId;

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { to: conv.userId },
      queryParamsHandling: 'merge'
    });

    this.isInitialLoad = true;
    this.unreadCount = 0;
    this.previousMessagesCount = 0;

    this.chatService.loadHistory(this.receiverId, this.myId);
  }

  sendMessage(): void {
    if (!this.newMessageContent.trim()) return;

    const sentContent = this.newMessageContent;
    this.chatService.sendMessage(this.receiverId, sentContent, this.myId);
    this.newMessageContent = '';

    this.unreadCount = 0; // Poništava samo desni plutajući bedž
    this.smartScrollToBottom(true);

    // Ažuriramo lastMessage i vreme SAMO za trenutni chat, bez osvežavanja cele liste sa API-ja
    const recClean = String(this.receiverId).trim().toLowerCase();
    const conv = this.conversations.find(c => c.userId.toLowerCase() === recClean);

    if (conv) {
      conv.lastMessage = sentContent;
      conv.lastMessageTime = new Date().toISOString();
      this.sortConversations(); // Stavlja tvoj aktivni chat na vrh
    }
  }

  scrollToBottomManual(): void {
    this.unreadCount = 0;
    this.cdr.detectChanges();

    if (this.scrollContainer) {
      this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
    }
  }

  onScroll(): void {
    if (this.isUserNearBottom()) {
      if (this.unreadCount !== 0) {
        this.unreadCount = 0;
        this.cdr.detectChanges();
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

  private smartScrollToBottom(force: boolean = false): void {
    setTimeout(() => {
      try {
        if (this.scrollContainer && (force || this.isUserNearBottom())) {
          this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
        }
      } catch (err) { }
    }, 50);
  }

  ngOnDestroy(): void {
    if (this.messageSub) this.messageSub.unsubscribe();
    if (this.routeSub) this.routeSub.unsubscribe();
  }
}
