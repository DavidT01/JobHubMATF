
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Chat.API.Models
{

    public class Message
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string ChatId { get; set; }

        public string SenderId { get; set; }

        public string Text { get; set; }

        public DateTime Timestamp { get; set; }

        public bool IsRead { get; set; } = false;

    }

}