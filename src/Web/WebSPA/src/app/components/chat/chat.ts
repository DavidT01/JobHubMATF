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

  constructor(
    private chatService: ChatService,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef,
    private ngZone: NgZone
  ) { }

  ngOnInit(): void {
    this.routeSub = this.route.queryParams.subscribe(params => {
      if (params['as']) {
        this.myId = params['as'];
        this.currentUserId = params['as'];
      }
      if (params['to']) {
        this.receiverId = params['to'];
      }

      this.isInitialLoad = true;
      this.unreadCount = 0;
      this.chatService.startConnection(this.myId);
      this.chatService.loadHistory(this.receiverId, this.myId);
    });

    this.messageSub = this.chatService.messages$.subscribe((messages) => {
      this.messages = messages;

      const lastMsg = messages[messages.length - 1];

      // Rešavamo NG0100 grešku: odlažemo izmenu stanja za sledeći tick
      setTimeout(() => {
        if (this.isInitialLoad) {
          this.smartScrollToBottom(true);
          this.isInitialLoad = false;
        } else {
          if (this.isUserNearBottom()) {
            this.smartScrollToBottom(false);
          } else {
            // Povećavamo brojač samo ako je poruku poslao DRUGI korisnik
            if (lastMsg && lastMsg.senderId !== this.myId) {
              this.unreadCount++;
            }
          }
        }
        this.cdr.detectChanges();
      }, 0);
    });
  }

  sendMessage(): void {
    if (!this.newMessageContent.trim()) return;

    this.chatService.sendMessage(this.myId, this.receiverId, this.newMessageContent);
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
    const threshold = 250;
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
