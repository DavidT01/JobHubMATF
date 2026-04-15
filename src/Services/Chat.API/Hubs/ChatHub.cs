using Chat.API.Services;
using Microsoft.AspNetCore.SignalR;
namespace Chat.API.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ChatService _chatService;

        public ChatHub(ChatService chatService)
        {
            _chatService = chatService;
        }
        public async Task SendMessage(string senderId, string reciverId, string message)
        {
            var savedMessage = await _chatService.SendMessageAsync(senderId, reciverId, message);
            await Clients.All.SendAsync("ReceiveMessage", 
                    savedMessage.SenderId,
                    reciverId,
                    savedMessage.Text,
                    savedMessage.Timestamp);
        }
    }
}
