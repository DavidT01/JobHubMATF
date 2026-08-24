import { Component, OnInit, OnDestroy, ChangeDetectorRef, ViewChild, ElementRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { ChatService, ChatMessage } from '../../services/chat';

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
  public newMessageContent: string = '';

  public myId: string = 'user1';
  public currentUserId: string = 'user1';
  public receiverId: string = 'user2';

  public unreadCount: number = 0;

  private messageSub!: Subscription;
  private routeSub!: Subscription;
  private isInitialLoad: boolean = true;
  private previousMessagesCount: number = 0;

  constructor(
    private chatService: ChatService,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef,
    private ngZone: NgZone
  ) { }

  ngOnInit(): void {
    this.chatService.startConnection();

    this.routeSub = this.route.queryParams.subscribe(params => {
      // Identitet pošiljaoca se uvek uzima iz tokena
      this.myId = this.chatService.getMyUserIdFromToken();
      this.currentUserId = this.myId;

      // Iz URL-a čitamo samo sa kim se dopisujemo
      if (params['to']) {
        this.receiverId = params['to'];
      } else {
        // Ako sam ja user1, default sagovornik je user2 i obrnuto
        this.receiverId = (this.myId.toLowerCase() === 'user1') ? 'user2' : 'user1';
      }

      this.isInitialLoad = true;
      this.unreadCount = 0;
      this.previousMessagesCount = 0;

      this.chatService.loadHistory(this.receiverId, this.myId);
    });

    this.messageSub = this.chatService.messages$.subscribe((allMessages) => {
      this.ngZone.run(() => {
        const myClean = String(this.myId).trim().toLowerCase();
        const recClean = String(this.receiverId).trim().toLowerCase();

        // Potpuno Case-Insensitive poređenje (user1 == USER1)
        const filtered = allMessages.filter(m => {
          const s = String(m.senderId || '').trim().toLowerCase();
          const r = String(m.receiverId || '').trim().toLowerCase();

          return (s === myClean && r === recClean) || (s === recClean && r === myClean);
        });

        const isNewMessageAdded = filtered.length > this.previousMessagesCount;
        this.messages = filtered;

        const lastMsg = this.messages[this.messages.length - 1];

        setTimeout(() => {
          if (this.isInitialLoad) {
            this.smartScrollToBottom(true);
            this.isInitialLoad = false;
          } else if (isNewMessageAdded && lastMsg) {
            const isFromOther = String(lastMsg.senderId).trim().toLowerCase() !== myClean;

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

  sendMessage(): void {
    if (!this.newMessageContent.trim()) return;

    // Prosleđujemo receiverId, sadržaj i TRENUTNI myId
    this.chatService.sendMessage(this.receiverId, this.newMessageContent, this.myId);
    this.newMessageContent = '';

    this.unreadCount = 0;
    this.smartScrollToBottom(true);
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
