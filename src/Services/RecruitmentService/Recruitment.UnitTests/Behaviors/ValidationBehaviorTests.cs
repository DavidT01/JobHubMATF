using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using Recruitment.API.Exceptions;
using Recruitment.API.Features.Behaviors;

namespace Recruitment.UnitTests.Behaviors
{
    public class ValidationBehaviorTests
    {
        public class TestRequest : IRequest<string> { }

        [Fact]
        public async Task Handle_NoValidators_CallsNext()
        {
            var behavior = new ValidationBehavior<TestRequest, string>([]);
            var nextCalled = false;

            var result = await behavior.Handle(new TestRequest(), _ =>
            {
                nextCalled = true;
                return Task.FromResult("ok");
            }, CancellationToken.None);

            nextCalled.Should().BeTrue();
            result.Should().Be("ok");
        }

        [Fact]
        public async Task Handle_ValidationFails_ThrowsRecruitmentValidationException()
        {
            var validatorMock = new Mock<IValidator<TestRequest>>();
            validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult([new ValidationFailure("Field", "Field is required")]));

            var behavior = new ValidationBehavior<TestRequest, string>([validatorMock.Object]);

            var act = () => behavior.Handle(new TestRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

            await act.Should().ThrowAsync<RecruitmentValidationException>();
        }

        [Fact]
        public async Task Handle_ValidationSucceeds_CallsNext()
        {
            var validatorMock = new Mock<IValidator<TestRequest>>();
            validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            var behavior = new ValidationBehavior<TestRequest, string>([validatorMock.Object]);

            var result = await behavior.Handle(new TestRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

            result.Should().Be("ok");
        }
    }
}
