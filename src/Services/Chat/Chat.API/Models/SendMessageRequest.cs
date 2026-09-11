namespace Chat.API.Models
{
    public class SendMessageRequest
    {
        public string SenderId { get; set; }
        public string ReciverId { get; set; }

        public string Text { get; set; }
    }
}
