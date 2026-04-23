
using Chat.API.Models;
using Chat.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Chat.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly ChatService _chatService;

        public MessagesController(ChatService chatService)
        {
            _chatService = chatService;
        }
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request) {
            if (string.IsNullOrEmpty(request.Text))
                return BadRequest("Message cannot be empty");

            var message = await _chatService.SendMessageAsync(
                request.SenderId,
                request.ReciverId,
                request.Text
            );

            return Ok(message);

        }

        [HttpGet]
        public async Task<IActionResult> GetMessages([FromQuery] string user1, [FromQuery] string user2)
        {
            if (string.IsNullOrEmpty(user1) || string.IsNullOrEmpty(user2))
                return BadRequest("User IDs are required");

            var messages = await _chatService.GetMessagesAsync(user1, user2);

            return Ok(messages);
        }

        [HttpGet("{chatId}")]
        public async Task<ActionResult<List<Message>>> GetMessagesByChatId(string chatId)
        {
            var messages = await _chatService.GetMessagesByChatIdAsync(chatId);
            return Ok(messages);
        }
    }
}
