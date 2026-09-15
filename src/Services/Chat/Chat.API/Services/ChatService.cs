using Chat.API.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Options;

namespace Chat.API.Services
{
    public class ChatService : IChatService
    {
        private readonly IMongoCollection<Message> _messages;
        private readonly IMongoCollection<Models.Chat> _chats;

        public ChatService(IMongoDatabase database)
        {
            _chats = database.GetCollection<Models.Chat>("Chats");
            _messages = database.GetCollection<Message>("Messages");

            CreateIndexes().GetAwaiter().GetResult();
        }

        public ChatService(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);

            _chats = database.GetCollection<Models.Chat>("Chats");
            _messages = database.GetCollection<Message>("Messages");

            CreateIndexes().GetAwaiter().GetResult();
        }

        private async Task CreateIndexes()
        {
            var chatIndexKeys = Builders<Models.Chat>.IndexKeys
                .Ascending(c => c.User1Id)
                .Ascending(c => c.User2Id);
            await _chats.Indexes.CreateOneAsync(new CreateIndexModel<Models.Chat>(chatIndexKeys));

            var messageIndexKeys = Builders<Message>.IndexKeys
                .Ascending(m => m.ChatId)
                .Descending(m => m.Timestamp);
            await _messages.Indexes.CreateOneAsync(new CreateIndexModel<Message>(messageIndexKeys));
        }

        public async Task<Models.Chat> GetOrCreateChatAsync(string currentUserId, string currentUserName, string currentUserRole, string targetUserId, string targetUserName)
        {
            var sortedUsers = new List<string> { currentUserId, targetUserId };
            sortedUsers.Sort();

            var u1 = sortedUsers[0];
            var u2 = sortedUsers[1];

            var user1Name = (u1 == currentUserId) ? currentUserName : targetUserName;
            var user2Name = (u2 == currentUserId) ? currentUserName : targetUserName;

            // 1. Ako chat već postoji, dozvoljavamo komunikaciju
            var chat = await _chats
                .Find(c => c.User1Id == u1 && c.User2Id == u2)
                .FirstOrDefaultAsync();

            if (chat != null)
            {
                // Upisujemo ime samo ako je "pravo" (nije prazno i nije placeholder = ID sagovornika),
                // da ne bismo pregazili već poznato ispravno ime sa privremenim/placeholder vrednostima.
                var updates = new List<UpdateDefinition<Models.Chat>>();

                var user1NameIsValid = !string.IsNullOrWhiteSpace(user1Name) && user1Name != u1;
                var user2NameIsValid = !string.IsNullOrWhiteSpace(user2Name) && user2Name != u2;

                if (user1NameIsValid)
                    updates.Add(Builders<Models.Chat>.Update.Set(c => c.User1Name, user1Name));

                if (user2NameIsValid)
                    updates.Add(Builders<Models.Chat>.Update.Set(c => c.User2Name, user2Name));

                if (updates.Count > 0)
                {
                    var combinedUpdate = Builders<Models.Chat>.Update.Combine(updates);
                    await _chats.UpdateOneAsync(c => c.Id == chat.Id, combinedUpdate);

                    if (user1NameIsValid) chat.User1Name = user1Name;
                    if (user2NameIsValid) chat.User2Name = user2Name;
                }

                return chat;
            }

            // 2. Ako chat NE postoji, samo poslodavac sme da inicira novi razgovor
            if (!string.Equals(currentUserRole, "Employer", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Samo korisnici sa ulogom poslodavca mogu započinjati nove razgovore.");
            }

            var newChat = new Models.Chat
            {
                User1Id = u1,
                User2Id = u2,
                User1Name = user1Name,
                User2Name = user2Name,
                CreatedAt = DateTime.UtcNow
            };

            await _chats.InsertOneAsync(newChat);
            return newChat;
        }

        public async Task<Message> SendMessageAsync(string senderId, string senderName, string senderRole, string receiverId, string receiverName, string text)
        {
            // Prosleđujemo ulogu i imena da bi GetOrCreateChatAsync mogao da proveri da li sme da kreira chat i da upiše imena
            var chat = await GetOrCreateChatAsync(senderId, senderName, senderRole, receiverId, receiverName);

            var message = new Message
            {
                ChatId = chat.Id,
                SenderId = senderId,
                Text = text,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            await _messages.InsertOneAsync(message);
            return message;
        }

        public async Task<List<Message>> GetMessagesAsync(string user1, string user2)
        {
            var sortedUsers = new List<string> { user1, user2 };
            sortedUsers.Sort();

            var u1 = sortedUsers[0];
            var u2 = sortedUsers[1];

            var chat = await _chats
                .Find(c => c.User1Id == u1 && c.User2Id == u2)
                .FirstOrDefaultAsync();

            if (chat == null)
                return new List<Message>();

            return await _messages
                .Find(m => m.ChatId == chat.Id)
                .SortBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task<List<Message>> GetMessagesByChatIdAsync(string chatId)
        {
            return await _messages
                .Find(m => m.ChatId == chatId)
                .SortBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task<List<ConversationDto>> GetUserConversationsAsync(string currentUserId)
        {
            var userChats = await _chats
                .Find(c => c.User1Id == currentUserId || c.User2Id == currentUserId)
                .ToListAsync();

            var conversations = new List<ConversationDto>();

            foreach (var chat in userChats)
            {
                var otherUserId = chat.User1Id == currentUserId ? chat.User2Id : chat.User1Id;
                var otherUserName = chat.User1Id == currentUserId ? chat.User2Name : chat.User1Name;

                var lastMessage = await _messages
                    .Find(m => m.ChatId == chat.Id)
                    .SortByDescending(m => m.Timestamp)
                    .FirstOrDefaultAsync();

                var unreadCount = (int)await _messages
                    .CountDocumentsAsync(m => m.ChatId == chat.Id
                                           && m.SenderId == otherUserId
                                           && m.IsRead == false);

                conversations.Add(new ConversationDto
                {
                    UserId = otherUserId,
                    UserName = string.IsNullOrEmpty(otherUserName) ? otherUserId : otherUserName,
                    LastMessage = lastMessage?.Text ?? "Nema poruka",
                    LastMessageTime = lastMessage?.Timestamp ?? chat.CreatedAt,
                    UnreadCount = unreadCount,
                    HasUnread = unreadCount > 0
                });
            }

            return conversations.OrderByDescending(c => c.LastMessageTime).ToList();
        }

        public async Task MarkAsReadAsync(string currentUserId, string currentUserRole, string otherUserId)
        {
            var chat = await GetOrCreateChatAsync(currentUserId, currentUserId, currentUserRole, otherUserId, otherUserId);

            var filter = Builders<Message>.Filter.And(
                Builders<Message>.Filter.Eq(m => m.ChatId, chat.Id),
                Builders<Message>.Filter.Eq(m => m.SenderId, otherUserId),
                Builders<Message>.Filter.Eq(m => m.IsRead, false)
            );

            var update = Builders<Message>.Update.Set(m => m.IsRead, true);

            await _messages.UpdateManyAsync(filter, update);
        }
    }
}