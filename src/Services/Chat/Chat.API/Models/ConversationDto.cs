using System.Text.Json.Serialization;

namespace Chat.API.Models
{
    public class ConversationDto
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("userName")]
        public string UserName { get; set; } = string.Empty;

        [JsonPropertyName("lastMessage")]
        public string LastMessage { get; set; } = string.Empty;

        [JsonPropertyName("lastMessageTime")]
        public DateTime LastMessageTime { get; set; }

        [JsonPropertyName("unreadCount")]
        public int UnreadCount { get; set; } = 0;

        [JsonPropertyName("hasUnread")]
        public bool HasUnread { get; set; }
    }
}