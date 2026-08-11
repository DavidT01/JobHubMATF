using Chat.API.Models;
using MongoDB.Driver;



namespace Chat.API.Services
{
    public class ChatService
    {
        private readonly IMongoCollection<Message> _messages;
        private readonly IMongoCollection<Models.Chat> _chats;

        public ChatService(IConfiguration config)
        {
            var settings = config.GetSection("MongoDbSettings").Get<MongoDbSettings>();
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _chats = database.GetCollection<Models.Chat>("Chats");
            _messages = database.GetCollection<Message>("Messages");
        }

        public async Task<Models.Chat> GetOrCreateChatAsync(string user1, string user2)
        {
            // Sortiranje userId-eva
            var sortedUsers = new List<string> { user1, user2 };
            sortedUsers.Sort();

            var u1 = sortedUsers[0];
            var u2 = sortedUsers[1];

            // Traženje postojećeg chata
            var chat = await _chats
                .Find(c => c.User1Id == u1 && c.User2Id == u2)
                .FirstOrDefaultAsync();

            if (chat != null)
                return chat;

            // Kreiranje novog chata
            var newChat = new Models.Chat
            {
                User1Id = u1,
                User2Id = u2,
                CreatedAt = DateTime.UtcNow
            };

            await _chats.InsertOneAsync(newChat);

            return newChat;
        }

        public async Task<Message> SendMessageAsync(string senderId, string receiverId, string text)
        {
            // 1. Uzmi ili kreiraj chat
            var chat = await GetOrCreateChatAsync(senderId, receiverId);

            
            // 2. Kreiraj poruku
            var message = new Message
            {
                ChatId = chat.Id,
                SenderId = senderId,
                Text = text,
                Timestamp = DateTime.UtcNow
            };

            // 3. Sačuvaj u bazu
            await _messages.InsertOneAsync(message);

            return message;
        }

        public async Task<List<Message>> GetMessagesAsync(string user1, string user2)
        {
            // 1. Sortiranje userId-eva
            var sortedUsers = new List<string> { user1, user2 };
            sortedUsers.Sort();

            var u1 = sortedUsers[0];
            var u2 = sortedUsers[1];

            // 2. Pronađi chat
            var chat = await _chats
                .Find(c => c.User1Id == u1 && c.User2Id == u2)
                .FirstOrDefaultAsync();

            if (chat == null)
                return new List<Message>();

            // 3. Uzmi sve poruke za taj chat
            var messages = await _messages
                .Find(m => m.ChatId == chat.Id)
                .SortBy(m => m.Timestamp)
                .ToListAsync();

            return messages;
        }

        public async Task<List<Message>> GetMessagesByChatIdAsync(string chatId)
        {
            return await _messages
                .Find(m => m.ChatId == chatId)
                .SortBy(m => m.Timestamp)
                .ToListAsync();
        }
    }
}
