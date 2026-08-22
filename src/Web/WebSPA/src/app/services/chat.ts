import { Injectable, NgZone } from '@angular/core'; // <-- DODAT NgZone
import * as signalR from '@microsoft/signalr';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';

export interface ChatMessage {
  id?: string;
  senderId: string;
  receiverId: string;
  content: string;
  timestamp?: Date;
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private hubConnection!: signalR.HubConnection;
  private gatewayUrl = 'http://localhost:5107';

  private messagesSubject = new BehaviorSubject<ChatMessage[]>([]);
  public messages$ = this.messagesSubject.asObservable();

  // Inject-ujemo NgZone u konstruktoru:
  constructor(private http: HttpClient, private ngZone: NgZone) { }

  public startConnection(userId: string, jwtToken: string = 'TVOJ_JWT_TOKEN'): void {
    if (this.hubConnection) {
      this.hubConnection.off('ReceiveMessage');
      this.hubConnection.stop();
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.gatewayUrl}/chatHub?userId=${userId}`, {
        accessTokenFactory: () => jwtToken,
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => console.log(`✅ SignalR Konekcija uspešno uspostavljena za korisnika: ${userId}`))
      .catch(err => console.error('❌ Greška pri konekciji sa SignalR-om: ', err));

    this.addMessageListener();
  }

  private addMessageListener(): void {
    this.hubConnection.off('ReceiveMessage');

    // Vraćamo 4 pojedinačna parametra kako ih tvoj ChatHub i šalje
    this.hubConnection.on('ReceiveMessage', (senderId: string, receiverId: string, content: string, timestamp: any) => {
      console.log('📩 Primljena poruka sa servera:', { senderId, receiverId, content, timestamp });

      const newMessage: ChatMessage = {
        senderId: senderId,
        receiverId: receiverId,
        content: content,
        timestamp: timestamp ? new Date(timestamp) : new Date()
      };

      this.ngZone.run(() => {
        const currentMessages = this.messagesSubject.value;
        this.messagesSubject.next([...currentMessages, newMessage]);
      });
    });
  }

  public sendMessage(senderId: string, receiverId: string, content: string): void {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      this.hubConnection.invoke('SendMessage', senderId, receiverId, content)
        .then(() => console.log('🚀 Poruka uspešno poslata preko Hub-a!'))
        .catch(err => console.error('❌ Greška pri pozivanju SendMessage na backendu:', err));
    } else {
      console.warn('⚠️ SignalR konekcija nije aktivna. Trenutno stanje:', this.hubConnection?.state);
    }
  }

  public loadHistory(otherUserId: string, myUserId: string, jwtToken: string = 'TVOJ_JWT_TOKEN'): void {
    const headers = new HttpHeaders({
      'Authorization': `Bearer ${jwtToken}`
    });

    // Gađamo tačan endpoint iz MessagesController-a
    this.http.get<any[]>(`${this.gatewayUrl}/api/messages?user1=${myUserId}&user2=${otherUserId}`, { headers })
      .subscribe({
        next: (response) => {
          console.log('📜 Istorija poruka iz baze:', response);

          // Mapiramo polja ako se imena na backendu razlikuju (npr. Text -> content, ReciverId -> receiverId)
          const historyArray: ChatMessage[] = (response || []).map((msg: any) => ({
            id: msg.id,
            senderId: msg.senderId,
            receiverId: msg.reciverId || msg.receiverId,
            content: msg.text || msg.content,
            timestamp: msg.timestamp || msg.createdAt
          }));

          this.ngZone.run(() => {
            this.messagesSubject.next(historyArray);
          });
        },
        error: (err) => console.error('❌ Greška pri učitavanju istorije:', err)
      });
  }
}
