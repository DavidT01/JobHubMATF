using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Chat.API.Services;
using Chat.API.Models;

namespace Chat.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        // Unificirana metoda za preuzimanje ID-ja korisnika iz JWT tokena
        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("id")?.Value
                ?? User.FindFirst("userId")?.Value
                ?? User.FindFirst(ClaimTypes.Name)?.Value;
        }

        // 1. Sidebar - lista svih konverzacija
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Nije moguće pročitati korisnički ID iz tokena.");

            var conversations = await _chatService.GetUserConversationsAsync(userId);
            return Ok(conversations);
        }

        // 2. Poruke - podržava i novu rutu (/api/chat/messages) i staru (/api/messages)
        [HttpGet("messages")]
        [HttpGet("/api/messages")]
        public async Task<IActionResult> GetMessages([FromQuery] string? otherUserId, [FromQuery] string? user1, [FromQuery] string? user2)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Ako Angular šalje ?user1=lazar&user2=predrag
            var targetUserId = otherUserId;
            if (string.IsNullOrEmpty(targetUserId))
            {
                targetUserId = (user1 == userId) ? user2 : user1;
            }

            if (string.IsNullOrEmpty(targetUserId))
                return BadRequest("ID sagovornika je obavezan.");

            var messages = await _chatService.GetMessagesAsync(userId, targetUserId);
            return Ok(messages);
        }

        // 3. Dohvatanje poruka po ID-ju chata
        [HttpGet("messages/chat/{chatId}")]
        public async Task<ActionResult<List<Message>>> GetMessagesByChatId(string chatId)
        {
            var messages = await _chatService.GetMessagesByChatIdAsync(chatId);
            return Ok(messages);
        }

        // 4. Slanje poruke preko HTTP API-ja - podržava obe rute
        [HttpPost("messages")]
        [HttpPost("/api/messages")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            var senderId = GetCurrentUserId();
            if (string.IsNullOrEmpty(senderId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest("Poruka ne može biti prazna.");

            var message = await _chatService.SendMessageAsync(
                senderId,
                request.ReciverId,
                request.Text
            );

            return Ok(message);
        }

        // 5. Označavanje poruka kao pročitane
        [HttpPost("mark-as-read/{otherUserId}")]
        public async Task<IActionResult> MarkAsRead(string otherUserId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            await _chatService.MarkAsReadAsync(userId, otherUserId);
            return Ok();
        }

        // 6. Provera trenutno ulogovanog korisnika
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                UserId = GetCurrentUserId(),
                Email = User.FindFirstValue(ClaimTypes.Email),
                Name = User.FindFirstValue(ClaimTypes.Name),
                Roles = User.FindAll(ClaimTypes.Role).Select(x => x.Value)
            });
        }
    }
}