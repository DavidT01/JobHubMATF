using FluentAssertions;
using FluentValidation;
using Profile.API.Exceptions;
using Profile.API.Features.Behaviors;

namespace Profile.UnitTests.Behaviors
{
    public class ValidationBehaviorTests
    {
        private sealed record TestRequest(string Value);

        [Fact]
        public async Task Handle_WithoutValidators_CallsNext()
        {
            var behavior = new ValidationBehavior<TestRequest, string>(Array.Empty<IValidator<TestRequest>>());
            var nextCalled = false;

            var result = await behavior.Handle(new TestRequest("valid"), _ =>
            {
                nextCalled = true;
                return Task.FromResult("ok");
            }, CancellationToken.None);

            result.Should().Be("ok");
            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_InvalidRequest_ThrowsProfileValidationException()
        {
            var validator = new InlineValidator<TestRequest>();
            validator.RuleFor(request => request.Value).NotEmpty();
            var behavior = new ValidationBehavior<TestRequest, string>(new[] { validator });

            var action = () => behavior.Handle(new TestRequest(""), _ => Task.FromResult("unexpected"), CancellationToken.None);

            var exception = await action.Should().ThrowAsync<ProfileValidationException>();
            exception.Which.Errors.Should().ContainKey(nameof(TestRequest.Value));
        }
    }
}
