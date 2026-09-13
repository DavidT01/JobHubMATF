using Chat.API.Models;

namespace Chat.API.Services
{
    public interface IChatService
    {
        Task<List<ConversationDto>> GetUserConversationsAsync(string userId);
        Task<List<Message>> GetMessagesAsync(string userId, string targetUserId);
        Task<List<Message>> GetMessagesByChatIdAsync(string chatId);
        Task<Message> SendMessageAsync(string senderId, string senderRole, string receiverId, string text);
        Task MarkAsReadAsync(string userId, string userRole, string otherUserId);
        Task<Models.Chat> GetOrCreateChatAsync(string currentUserId, string currentUserRole, string targetUserId);
    }
}