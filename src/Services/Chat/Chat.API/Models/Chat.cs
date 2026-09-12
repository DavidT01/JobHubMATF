using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace Chat.API.Models
{

    public class Chat
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]

        public string Id { get; set; }

        public string User1Id { get; set; }

        public string User2Id { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}