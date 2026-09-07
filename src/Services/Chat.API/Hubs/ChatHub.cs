using System.Security.Claims;
using Chat.API.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Concurrent;

namespace Chat.API.Hubs
{
    [Authorize(Roles = "Candidate,Employer,Admin")]
    public class ChatHub : Hub
    {
        private readonly ChatService _chatService;
        private static readonly ConcurrentDictionary<string, string> _connections = new();

        public ChatHub(ChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task SendMessage(string reciverId, string message)
        {
            // 1. Izvlačenje senderId iz JWT tokena ili Query-ja
            var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? Context.User?.FindFirst("sub")?.Value
                        ?? Context.User?.FindFirst("nameid")?.Value
                        ?? Context.GetHttpContext()?.Request.Query["userId"].ToString();

            if (string.IsNullOrEmpty(senderId))
            {
                // Fallback na default vrednost za testiranje ako nema tokena
                senderId = "user1";
            }

            // 2. Čuvanje u MongoDB bazi preko ChatService-a
            var savedMessage = await _chatService.SendMessageAsync(senderId, reciverId, message);

            // 3. Slanje primaocu (ako je povezan)
            if (_connections.TryGetValue(reciverId, out var receiverConnection))
            {
                await Clients.Client(receiverConnection).SendAsync("ReceiveMessage",
                    senderId,
                    reciverId,
                    savedMessage.Text,
                    savedMessage.Timestamp);
            }

            // 4. Slanje nazad pošiljaocu za potvrdu
            if (_connections.TryGetValue(senderId, out var senderConnection) && senderConnection != receiverConnection)
            {
                await Clients.Client(senderConnection).SendAsync("ReceiveMessage",
                    senderId,
                    reciverId,
                    savedMessage.Text,
                    savedMessage.Timestamp);
            }
        }

        public override Task OnConnectedAsync()
        {
            // Detekcija ID-ja iz JWT tokena ili query string-a pri spajanju
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? Context.User?.FindFirst("sub")?.Value
                      ?? Context.User?.FindFirst("nameid")?.Value
                      ?? Context.GetHttpContext()?.Request.Query["userId"].ToString();

            if (!string.IsNullOrEmpty(userId))
            {
                _connections.AddOrUpdate(userId, Context.ConnectionId, (key, oldValue) => Context.ConnectionId);
            }

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var item = _connections.FirstOrDefault(x => x.Value == Context.ConnectionId);

            if (!string.IsNullOrEmpty(item.Key))
            {
                _connections.TryRemove(item.Key, out _);
            }

            return base.OnDisconnectedAsync(exception);
        }
    }
}