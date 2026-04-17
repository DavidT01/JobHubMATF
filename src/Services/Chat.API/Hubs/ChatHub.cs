using Chat.API.Services;
using Microsoft.AspNetCore.SignalR;
namespace Chat.API.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ChatService _chatService;
        private static readonly Dictionary<string, string> _connections = new();

        public ChatHub(ChatService chatService)
        {
            _chatService = chatService;
        }
        public async Task SendMessage(string senderId, string reciverId, string message)
        {
            var savedMessage = await _chatService.SendMessageAsync(senderId, reciverId, message);
            if(_connections.TryGetValue(reciverId, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", 
                    savedMessage.SenderId,
                    reciverId,
                    savedMessage.Text,
                    savedMessage.Timestamp);
            }

            if (_connections.TryGetValue(senderId, out var senderConnection))
            {
                await Clients.Client(senderConnection).SendAsync("ReceiveMessage",
                    savedMessage.SenderId,
                    reciverId,
                    savedMessage.Text,
                    savedMessage.Timestamp);
            }
        }
        public override Task OnConnectedAsync()
        {
            var userId = Context.GetHttpContext().Request.Query["userId"];

            if (!string.IsNullOrEmpty(userId))
            {
                _connections[userId] = Context.ConnectionId;
            }

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var item = _connections.FirstOrDefault(x => x.Value == Context.ConnectionId);

            if (!string.IsNullOrEmpty(item.Key))
            {
                _connections.Remove(item.Key);
            }

            return base.OnDisconnectedAsync(exception);
        }
    }
}
