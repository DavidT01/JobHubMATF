import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core'; // <-- DODAT ChangeDetectorRef
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
  styleUrl: './chat.scss'
})
export class ChatComponent implements OnInit, OnDestroy {
  public messages: ChatMessage[] = [];
  public newMessageContent: string = '';

  public myId: string = 'user1';
  public currentUserId: string = 'user1';
  public receiverId: string = 'user2';

  private messageSub!: Subscription;
  private routeSub!: Subscription;

  constructor(
    private chatService: ChatService,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef // <-- INJECT CD REF
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

      this.chatService.startConnection(this.myId);
      this.chatService.loadHistory(this.receiverId, this.myId);
    });

    // Pretplata na nove poruke
    this.messageSub = this.chatService.messages$.subscribe((messages) => {
      this.messages = messages;

      // FORSIRAMO ANGULAR DA ODMAH PRECRTA EKRAN!
      this.cdr.detectChanges();
    });
  }

  sendMessage(): void {
    if (!this.newMessageContent.trim()) return;

    this.chatService.sendMessage(this.myId, this.receiverId, this.newMessageContent);
    this.newMessageContent = '';
  }

  ngOnDestroy(): void {
    if (this.messageSub) this.messageSub.unsubscribe();
    if (this.routeSub) this.routeSub.unsubscribe();
  }
}
