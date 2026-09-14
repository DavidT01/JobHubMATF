using System.Security.Claims;
using Chat.API.Controllers;
using Chat.API.Models;
using Chat.API.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Chat.UnitTests.Controllers;

public class ChatControllerTests
{
    private readonly Mock<IChatService> _chatServiceMock;

    public ChatControllerTests()
    {
        // Mock-ujemo isključivo IChatService interfejs
        _chatServiceMock = new Mock<IChatService>();
    }

    private ChatController CreateControllerWithUser(string userId, string role = "Employer")
    {
        var controller = new ChatController(_chatServiceMock.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        return controller;
    }

    [Fact]
    public async Task SendMessage_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var senderId = "user-1";
        var senderRole = "Employer";
        var request = new SendMessageRequest
        {
            ReciverId = "user-2",
            Text = "Zdravo iz testa!"
        };

        _chatServiceMock
            .Setup(s => s.SendMessageAsync(senderId, senderRole, request.ReciverId, request.Text))
            .ReturnsAsync(new Message
            {
                Id = "msg-1",
                SenderId = senderId,
                Text = request.Text,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            });

        var controller = CreateControllerWithUser(senderId, senderRole);

        // Act
        var result = await controller.SendMessage(request);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task SendMessage_ShouldReturnBadRequest_WhenTextIsEmpty()
    {
        // Arrange
        var controller = CreateControllerWithUser("user-1");
        var request = new SendMessageRequest
        {
            ReciverId = "user-2",
            Text = "" // Prazna poruka koja aktivira proveru u kontroleru
        };

        // Act
        var result = await controller.SendMessage(request);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetMessages_ShouldReturnOk_WhenParametersAreValid()
    {
        // Arrange
        var currentUserId = "user-1";
        var otherUserId = "user-2";

        var expectedMessages = new List<Message>
        {
            new Message { Id = "msg-1", ChatId = "chat-1", SenderId = currentUserId, Text = "Zdravo!" },
            new Message { Id = "msg-2", ChatId = "chat-1", SenderId = otherUserId, Text = "Ćao!" }
        };

        _chatServiceMock
            .Setup(s => s.GetMessagesAsync(currentUserId, otherUserId))
            .ReturnsAsync(expectedMessages);

        var controller = CreateControllerWithUser(currentUserId);

        // Act
        var result = await controller.GetMessages(otherUserId, null, null);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var messages = okResult.Value as List<Message>;
        messages.Should().NotBeNull();
        messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMessages_ShouldReturnBadRequest_WhenTargetUserIdIsMissing()
    {
        // Arrange
        var currentUserId = "user-1";
        var controller = CreateControllerWithUser(currentUserId);

        // Act - Prosleđujemo sve parametre kao null da izazovemo BadRequest
        var result = await controller.GetMessages(null, null, null);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetConversations_ShouldReturnOk_WhenUserIsValid()
    {
        // Arrange
        var currentUserId = "user-1";
        var expectedConversations = new List<ConversationDto>
        {
            new ConversationDto
            {
                UserId = "user-2",
                UserName = "user-2",
                LastMessage = "Zdravo!",
                UnreadCount = 0
            }
        };

        _chatServiceMock
            .Setup(s => s.GetUserConversationsAsync(currentUserId))
            .ReturnsAsync(expectedConversations);

        var controller = CreateControllerWithUser(currentUserId);

        // Act
        var result = await controller.GetConversations();

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var conversations = okResult.Value as List<ConversationDto>;
        conversations.Should().NotBeNull();
        conversations.Should().HaveCount(1);
    }

    [Fact]
    public async Task MarkAsRead_ShouldReturnOk_WhenParametersAreValid()
    {
        // Arrange
        var currentUserId = "user-1";
        var currentUserRole = "Employer";
        var otherUserId = "user-2";

        _chatServiceMock
            .Setup(s => s.MarkAsReadAsync(currentUserId, currentUserRole, otherUserId))
            .Returns(Task.CompletedTask);

        var controller = CreateControllerWithUser(currentUserId, currentUserRole);

        // Act
        var result = await controller.MarkAsRead(otherUserId);

        // Assert
        var okResult = result as OkResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetMessagesByChatId_ShouldReturnOk_WhenChatIdIsValid()
    {
        // Arrange
        var chatId = "chat-123";
        var expectedMessages = new List<Message>
        {
            new Message { Id = "msg-1", ChatId = chatId, SenderId = "user-1", Text = "Zdravo!" }
        };

        _chatServiceMock
            .Setup(s => s.GetMessagesByChatIdAsync(chatId))
            .ReturnsAsync(expectedMessages);

        var controller = CreateControllerWithUser("user-1");

        // Act
        var result = await controller.GetMessagesByChatId(chatId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var messages = okResult.Value as List<Message>;
        messages.Should().NotBeNull();
        messages.Should().HaveCount(1);
    }
}