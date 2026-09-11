using Chat.API.Models;
using Chat.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Chat.UnitTests.Services;

public class ChatServiceTests
{
    private readonly Mock<IMongoDatabase> _databaseMock;
    private readonly Mock<IMongoCollection<Message>> _messagesCollectionMock;
    private readonly Mock<IMongoCollection<API.Models.Chat>> _chatsCollectionMock;

    public ChatServiceTests()
    {
        _databaseMock = new Mock<IMongoDatabase>();
        _messagesCollectionMock = new Mock<IMongoCollection<Message>>();
        _chatsCollectionMock = new Mock<IMongoCollection<API.Models.Chat>>();

        _databaseMock
            .Setup(db => db.GetCollection<API.Models.Chat>("Chats", null))
            .Returns(_chatsCollectionMock.Object);

        _databaseMock
            .Setup(db => db.GetCollection<Message>("Messages", null))
            .Returns(_messagesCollectionMock.Object);

        _chatsCollectionMock
            .Setup(c => c.Indexes.CreateOneAsync(It.IsAny<CreateIndexModel<API.Models.Chat>>(), null, default))
            .ReturnsAsync("index");

        _messagesCollectionMock
            .Setup(c => c.Indexes.CreateOneAsync(It.IsAny<CreateIndexModel<Message>>(), null, default))
            .ReturnsAsync("index");
    }

    [Fact]
    public async Task GetOrCreateChatAsync_ShouldReturnExistingChat_WhenChatAlreadyExists()
    {
        // Arrange
        var user1 = "alice";
        var user2 = "bob";
        var existingChat = new API.Models.Chat
        {
            Id = "chat-1",
            User1Id = "alice",
            User2Id = "bob",
            CreatedAt = DateTime.UtcNow
        };

        var asyncCursorMock = new Mock<IAsyncCursor<API.Models.Chat>>();
        asyncCursorMock.Setup(c => c.Current).Returns(new List<API.Models.Chat> { existingChat });
        asyncCursorMock
            .SetupSequence(c => c.MoveNextAsync(default))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _chatsCollectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<API.Models.Chat>>(),
                It.IsAny<FindOptions<API.Models.Chat, API.Models.Chat>>(),
                default))
            .ReturnsAsync(asyncCursorMock.Object);

        var service = new ChatService(_databaseMock.Object);

        // Act - ako chat postoji, uloga nije bitna za restrikciju, ali je prosleđujemo
        var result = await service.GetOrCreateChatAsync("alice", "Candidate", "bob");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("chat-1");
        result.User1Id.Should().Be("alice");
        result.User2Id.Should().Be("bob");
    }

    [Fact]
    public async Task GetOrCreateChatAsync_ShouldThrowUnauthorized_WhenChatDoesNotExistAndUserIsNotEmployer()
    {
        // Arrange
        var asyncCursorMock = new Mock<IAsyncCursor<API.Models.Chat>>();
        asyncCursorMock.Setup(c => c.Current).Returns(new List<API.Models.Chat>());
        asyncCursorMock
            .SetupSequence(c => c.MoveNextAsync(default))
            .ReturnsAsync(false); // Chat ne postoji

        _chatsCollectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<API.Models.Chat>>(),
                It.IsAny<FindOptions<API.Models.Chat, API.Models.Chat>>(),
                default))
            .ReturnsAsync(asyncCursorMock.Object);

        var service = new ChatService(_databaseMock.Object);

        // Act & Assert - Pokušava da kreira chat neko ko NIJE Employer
        var act = async () => await service.GetOrCreateChatAsync("candidate-1", "Candidate", "employer-1");
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Samo korisnici sa ulogom poslodavca mogu započinjati nove razgovore.");
    }

    [Fact]
    public async Task GetMessagesByChatIdAsync_ShouldReturnMessages_WhenChatIdIsValid()
    {
        var chatId = "chat-123";
        var expectedMessages = new List<Message>
        {
            new Message { Id = "m1", ChatId = chatId, Text = "Zdravo!" }
        };

        var asyncCursorMock = new Mock<IAsyncCursor<Message>>();
        asyncCursorMock.Setup(c => c.Current).Returns(expectedMessages);
        asyncCursorMock
            .SetupSequence(c => c.MoveNextAsync(default))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _messagesCollectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Message>>(),
                It.IsAny<FindOptions<Message, Message>>(),
                default))
            .ReturnsAsync(asyncCursorMock.Object);

        var service = new ChatService(_databaseMock.Object);

        var result = await service.GetMessagesByChatIdAsync(chatId);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Text.Should().Be("Zdravo!");
    }

    [Fact]
    public async Task MarkAsReadAsync_ShouldUpdateMessagesToRead()
    {
        var currentUserId = "alice";
        var currentUserRole = "Employer";
        var otherUserId = "bob";
        var chat = new API.Models.Chat { Id = "chat-1", User1Id = "alice", User2Id = "bob" };

        var chatCursorMock = new Mock<IAsyncCursor<API.Models.Chat>>();
        chatCursorMock.Setup(c => c.Current).Returns(new List<API.Models.Chat> { chat });
        chatCursorMock.SetupSequence(c => c.MoveNextAsync(default)).ReturnsAsync(true).ReturnsAsync(false);

        _chatsCollectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<API.Models.Chat>>(),
                It.IsAny<FindOptions<API.Models.Chat, API.Models.Chat>>(),
                default))
            .ReturnsAsync(chatCursorMock.Object);

        _messagesCollectionMock
            .Setup(c => c.UpdateManyAsync(
                It.IsAny<FilterDefinition<Message>>(),
                It.IsAny<UpdateDefinition<Message>>(),
                null, default))
            .ReturnsAsync(Mock.Of<UpdateResult>());

        var service = new ChatService(_databaseMock.Object);

        var act = async () => await service.MarkAsReadAsync(currentUserId, currentUserRole, otherUserId);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendMessageAsync_ShouldCreateChatAndInsertMessage_WhenValidEmployerDataProvided()
    {
        // Arrange
        var senderId = "employer-1";
        var senderRole = "Employer"; // Dozvoljeno kreiranje novog chata
        var receiverId = "candidate-1";
        var text = "Hello Candidate!";

        var chatCursorMock = new Mock<IAsyncCursor<API.Models.Chat>>();
        chatCursorMock.Setup(c => c.Current).Returns(new List<API.Models.Chat>()); // Nema postojećeg chata, kreiraće se
        chatCursorMock.SetupSequence(c => c.MoveNextAsync(default)).ReturnsAsync(false);

        _chatsCollectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<API.Models.Chat>>(),
                It.IsAny<FindOptions<API.Models.Chat, API.Models.Chat>>(),
                default))
            .ReturnsAsync(chatCursorMock.Object);

        _chatsCollectionMock
            .Setup(c => c.InsertOneAsync(It.IsAny<API.Models.Chat>(), null, default))
            .Returns(Task.CompletedTask);

        _messagesCollectionMock
            .Setup(c => c.InsertOneAsync(
                It.IsAny<Message>(),
                It.IsAny<InsertOneOptions>(),
                default))
            .Returns(Task.CompletedTask);

        var service = new ChatService(_databaseMock.Object);

        // Act
        var result = await service.SendMessageAsync(senderId, senderRole, receiverId, text);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Be(text);
        result.SenderId.Should().Be(senderId);
        result.IsRead.Should().BeFalse();

        _messagesCollectionMock.Verify(
            x => x.InsertOneAsync(It.Is<Message>(m => m.Text == text && m.SenderId == senderId), null, default),
            Times.Once);
    }

    [Fact]
    public async Task GetMessagesAsync_ShouldReturnMessages_WhenChatExists()
    {
        var user1 = "alice";
        var user2 = "bob";
        var chat = new API.Models.Chat { Id = "chat-1", User1Id = "alice", User2Id = "bob" };
        var expectedMessages = new List<Message>
        {
            new Message { Id = "m1", ChatId = "chat-1", Text = "Ćao!", Timestamp = DateTime.UtcNow.AddMinutes(-5) }
        };

        var chatCursorMock = new Mock<IAsyncCursor<API.Models.Chat>>();
        chatCursorMock.Setup(c => c.Current).Returns(new List<API.Models.Chat> { chat });
        chatCursorMock.SetupSequence(c => c.MoveNextAsync(default)).ReturnsAsync(true).ReturnsAsync(false);

        _chatsCollectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<API.Models.Chat>>(),
                It.IsAny<FindOptions<API.Models.Chat, API.Models.Chat>>(),
                default))
            .ReturnsAsync(chatCursorMock.Object);

        var messageCursorMock = new Mock<IAsyncCursor<Message>>();
        messageCursorMock.Setup(c => c.Current).Returns(expectedMessages);
        messageCursorMock.SetupSequence(c => c.MoveNextAsync(default)).ReturnsAsync(true).ReturnsAsync(false);

        _messagesCollectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Message>>(),
                It.IsAny<FindOptions<Message, Message>>(),
                default))
            .ReturnsAsync(messageCursorMock.Object);

        var service = new ChatService(_databaseMock.Object);

        var result = await service.GetMessagesAsync(user1, user2);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Text.Should().Be("Ćao!");
    }

    [Fact]
    public async Task GetUserConversationsAsync_ShouldReturnConversations_WhenChatsExist()
    {
        var currentUserId = "alice";
        var otherUserId = "bob";
        var chat = new API.Models.Chat { Id = "chat-1", User1Id = "alice", User2Id = "bob", CreatedAt = DateTime.UtcNow };
        var lastMessage = new Message { Id = "m1", ChatId = "chat-1", SenderId = "bob", Text = "Zdravo!", Timestamp = DateTime.UtcNow };

        var chatCursorMock = new Mock<IAsyncCursor<API.Models.Chat>>();
        chatCursorMock.Setup(c => c.Current).Returns(new List<API.Models.Chat> { chat });
        chatCursorMock.SetupSequence(c => c.MoveNextAsync(default)).ReturnsAsync(true).ReturnsAsync(false);

        _chatsCollectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<API.Models.Chat>>(),
                It.IsAny<FindOptions<API.Models.Chat, API.Models.Chat>>(),
                default))
            .ReturnsAsync(chatCursorMock.Object);

        var messageCursorMock = new Mock<IAsyncCursor<Message>>();
        messageCursorMock.Setup(c => c.Current).Returns(new List<Message> { lastMessage });
        messageCursorMock.SetupSequence(c => c.MoveNextAsync(default)).ReturnsAsync(true).ReturnsAsync(false);

        _messagesCollectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Message>>(),
                It.IsAny<FindOptions<Message, Message>>(),
                default))
            .ReturnsAsync(messageCursorMock.Object);

        _messagesCollectionMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Message>>(),
                It.IsAny<CountOptions>(),
                default))
            .ReturnsAsync(1L);

        var service = new ChatService(_databaseMock.Object);

        var result = await service.GetUserConversationsAsync(currentUserId);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].UserId.Should().Be(otherUserId);
        result[0].LastMessage.Should().Be("Zdravo!");
        result[0].UnreadCount.Should().Be(1);
        result[0].HasUnread.Should().BeTrue();
    }
}