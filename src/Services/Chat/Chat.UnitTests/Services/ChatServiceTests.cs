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

        // Ignorišemo kreiranje indeksa u konstruktoru
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

        // Act
        var result = await service.GetOrCreateChatAsync(user2, user1);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("chat-1");
        result.User1Id.Should().Be("alice");
        result.User2Id.Should().Be("bob");
    }

    [Fact]
    public async Task GetMessagesByChatIdAsync_ShouldReturnMessages_WhenChatIdIsValid()
    {
        // Arrange
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

        // Act
        var result = await service.GetMessagesByChatIdAsync(chatId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Text.Should().Be("Zdravo!");
    }

    [Fact]
    public async Task MarkAsReadAsync_ShouldUpdateMessagesToRead()
    {
        // Arrange
        var currentUserId = "alice";
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

        // Act & Assert
        var act = async () => await service.MarkAsReadAsync(currentUserId, otherUserId);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendMessageAsync_ShouldCreateChatAndInsertMessage_WhenValidDataProvided()
    {
        // Arrange
        var senderId = "alice";
        var receiverId = "bob";
        var text = "Hello Bob!";
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
            .Setup(c => c.InsertOneAsync(
                It.IsAny<Message>(),
                It.IsAny<InsertOneOptions>(),
                default))
            .Returns(Task.CompletedTask);

        var service = new ChatService(_databaseMock.Object);

        // Act
        var result = await service.SendMessageAsync(senderId, receiverId, text);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Be(text);
        result.SenderId.Should().Be(senderId);
        result.ChatId.Should().Be("chat-1");
        result.IsRead.Should().BeFalse();

        _messagesCollectionMock.Verify(
            x => x.InsertOneAsync(It.Is<Message>(m => m.Text == text && m.SenderId == senderId), null, default),
            Times.Once);
    }

    [Fact]
    public async Task GetMessagesAsync_ShouldReturnMessages_WhenChatExists()
    {
        // Arrange
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

        // Act
        var result = await service.GetMessagesAsync(user1, user2);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Text.Should().Be("Ćao!");
    }

    [Fact]
    public async Task GetUserConversationsAsync_ShouldReturnConversations_WhenChatsExist()
    {
        // Arrange
        var currentUserId = "alice";
        var otherUserId = "bob";
        var chat = new API.Models.Chat { Id = "chat-1", User1Id = "alice", User2Id = "bob", CreatedAt = DateTime.UtcNow };
        var lastMessage = new Message { Id = "m1", ChatId = "chat-1", SenderId = "bob", Text = "Zdravo!", Timestamp = DateTime.UtcNow };

        // Mock za listu chatu-ova korisnika
        var chatCursorMock = new Mock<IAsyncCursor<API.Models.Chat>>();
        chatCursorMock.Setup(c => c.Current).Returns(new List<API.Models.Chat> { chat });
        chatCursorMock.SetupSequence(c => c.MoveNextAsync(default)).ReturnsAsync(true).ReturnsAsync(false);

        _chatsCollectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<API.Models.Chat>>(),
                It.IsAny<FindOptions<API.Models.Chat, API.Models.Chat>>(),
                default))
            .ReturnsAsync(chatCursorMock.Object);

        // Mock za nalaženje poslednje poruke (FirstOrDefaultAsync koristi kursor)
        var messageCursorMock = new Mock<IAsyncCursor<Message>>();
        messageCursorMock.Setup(c => c.Current).Returns(new List<Message> { lastMessage });
        messageCursorMock.SetupSequence(c => c.MoveNextAsync(default)).ReturnsAsync(true).ReturnsAsync(false);

        _messagesCollectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Message>>(),
                It.IsAny<FindOptions<Message, Message>>(),
                default))
            .ReturnsAsync(messageCursorMock.Object);

        // Mock za brojanje nepročitanih poruka
        _messagesCollectionMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Message>>(),
                It.IsAny<CountOptions>(),
                default))
            .ReturnsAsync(1L);

        var service = new ChatService(_databaseMock.Object);

        // Act
        var result = await service.GetUserConversationsAsync(currentUserId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].UserId.Should().Be(otherUserId);
        result[0].LastMessage.Should().Be("Zdravo!");
        result[0].UnreadCount.Should().Be(1);
        result[0].HasUnread.Should().BeTrue();
    }
}