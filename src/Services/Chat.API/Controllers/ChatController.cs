using Microsoft.AspNetCore.Mvc;
using Chat.API.Services;
using Chat.API.Models;

namespace Chat.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ChatService _chatService;

        public ChatController(ChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpGet("{user1}/{user2}")]
        public async Task<ActionResult<Models.Chat>> GetChat(string user1, string user2)
        {
            var chat = await _chatService.GetOrCreateChatAsync(user1, user2);
            return Ok(chat);
        }
    }
}