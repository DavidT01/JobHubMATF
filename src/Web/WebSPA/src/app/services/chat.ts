import { Injectable, NgZone } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';

export interface ChatMessage {
  id?: string;
  senderId: string;
  receiverId: string;
  content: string;
  timestamp?: Date;
}

export interface Conversation {
  userId: string;
  userName: string;
  lastMessage: string;
  lastMessageTime: string;
  unreadCount?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private hubConnection!: HubConnection;
  private gatewayUrl = 'http://localhost:5107';

  private messagesSubject = new BehaviorSubject<ChatMessage[]>([]);
  public messages$: Observable<ChatMessage[]> = this.messagesSubject.asObservable();

  constructor(private http: HttpClient, private ngZone: NgZone) { }

  public startConnection(): void {
    if (this.hubConnection && this.hubConnection.state !== 'Disconnected') return;

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${this.gatewayUrl}/chatHub`, {
        accessTokenFactory: () => localStorage.getItem('jwt_token') || ''
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start()
      .then(() => {
        console.log('✅ SignalR konekcija uspostavljena.');
        this.addMessageListener();
      })
      .catch(err => console.error('❌ Greška pri konekciji:', err));
  }

  private addMessageListener(): void {
    this.hubConnection.off('ReceiveMessage');

    this.hubConnection.on('ReceiveMessage', (senderId: string, receiverId: string, content: string, timestamp: any) => {
      const newMessage: ChatMessage = {
        senderId: String(senderId).trim(),
        receiverId: String(receiverId).trim(),
        content: content,
        timestamp: timestamp ? new Date(timestamp) : new Date()
      };

      this.ngZone.run(() => {
        const current = this.messagesSubject.value;
        // Dodajemo samo ako poruka već ne postoji
        this.messagesSubject.next([...current, newMessage]);
      });
    });
  }

  public sendMessage(receiverId: string, content: string, currentMyId: string): void {
    if (this.hubConnection && this.hubConnection.state === 'Connected') {
      this.hubConnection.invoke('SendMessage', receiverId, content)
        .then(() => {
          console.log('🚀 Poruka poslata na server.');
        })
        .catch(err => console.error('❌ Greška pri slanju poruke:', err));
    } else {
      console.warn('⚠️ SignalR konekcija nije aktivna.');
    }
  }

  public loadHistory(otherUserId: string, myUserId: string): void {
    const token = localStorage.getItem('jwt_token') || '';
    const headers = new HttpHeaders({ 'Authorization': `Bearer ${token}` });

    const cleanMy = String(myUserId).trim();
    const cleanOther = String(otherUserId).trim();

    const url = `${this.gatewayUrl}/api/messages?user1=${cleanMy}&user2=${cleanOther}`;

    this.http.get<any[]>(url, { headers })
      .subscribe({
        next: (response) => {
          const historyArray: ChatMessage[] = (response || []).map((msg: any) => {
            const rawSender = msg.senderId || msg.sender;
            const calculatedReceiver = (String(rawSender).toLowerCase() === cleanMy.toLowerCase()) ? cleanOther : cleanMy;

            return {
              id: msg.id || msg._id,
              senderId: String(rawSender).trim(),
              receiverId: String(msg.receiverId || msg.reciverId || calculatedReceiver).trim(),
              content: msg.text || msg.content || '',
              timestamp: msg.timestamp || msg.createdAt ? new Date(msg.timestamp || msg.createdAt) : new Date()
            };
          });

          this.ngZone.run(() => {
            this.messagesSubject.next(historyArray);
          });
        },
        error: (err) => console.error('❌ Greška pri učitavanju istorije:', err)
      });
  }

  // --- METODA ZA DOWLAČENJE KONVERZACIJA ZA SIDEBAR ---
  public getConversations(): Observable<Conversation[]> {
    const token = localStorage.getItem('jwt_token') || '';
    const headers = new HttpHeaders({ 'Authorization': `Bearer ${token}` });
    const url = `${this.gatewayUrl}/api/chat/conversations`;

    return this.http.get<Conversation[]>(url, { headers });
  }

  public getMyUserIdFromToken(): string {
    const token = localStorage.getItem('jwt_token');
    if (!token) return 'user1';

    try {
      const payloadBase64 = token.split('.')[1];
      const decodedJson = atob(payloadBase64);
      const decoded = JSON.parse(decodedJson);

      return decoded.sub ||
        decoded.nameid ||
        decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ||
        decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ||
        'user1';
    } catch (e) {
      console.error('Greška pri čitanju tokena:', e);
      return 'user1';
    }
  }
}
