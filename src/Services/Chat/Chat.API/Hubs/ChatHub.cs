using System.Security.Claims;
using Chat.API.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Concurrent;

namespace Chat.API.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ChatService _chatService;
        private static readonly ConcurrentDictionary<string, string> _connections = new();

        public ChatHub(ChatService chatService)
        {
            _chatService = chatService;
        }

        private string? GetUserIdFromClaims()
        {
            var user = Context.User;
            if (user == null) return null;

            // Proveravamo sve standardne claim-ove koji mogu nositi korisnički ID
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value
                ?? user.FindFirst("id")?.Value
                ?? user.FindFirst("userId")?.Value
                ?? user.FindFirst(ClaimTypes.Name)?.Value;
        }

        public async Task SendMessage(string reciverId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            var senderId = GetUserIdFromClaims();

            if (string.IsNullOrEmpty(senderId))
            {
                throw new HubException("Korisnik nije autentifikovan.");
            }

            var senderRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value
                          ?? Context.User?.FindFirst("role")?.Value
                          ?? "Candidate";

            var savedMessage = await _chatService.SendMessageAsync(senderId, senderRole, reciverId, message);

            if (_connections.TryGetValue(reciverId, out var receiverConnection))
            {
                await Clients.Client(receiverConnection).SendAsync("ReceiveMessage",
                    senderId,
                    reciverId,
                    savedMessage.Text,
                    savedMessage.Timestamp);
            }

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
            var userId = GetUserIdFromClaims();

            if (!string.IsNullOrEmpty(userId))
            {
                _connections.AddOrUpdate(userId, Context.ConnectionId, (key, oldValue) => Context.ConnectionId);
            }

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserIdFromClaims();

            if (!string.IsNullOrEmpty(userId))
            {
                _connections.TryRemove(userId, out _);
            }

            return base.OnDisconnectedAsync(exception);
        }
    }
}