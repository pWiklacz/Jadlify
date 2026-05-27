using FluentValidation;
using Jadlify.Application.Common.Behaviours;
using Jadlify.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jadlify.Application.Tests.Common.Behaviours;

public class ValidationBehaviorTests
{
    private record TestRequest(string Name);
    private record TestResponse(string Value);

    private class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name cannot be empty");
        }
    }

    [Fact]
    public async Task HandleAsync_ShouldCallNext_WhenValidationSucceeds()
    {
        // Arrange
        TestRequestValidator[] validators = new[] { new TestRequestValidator() };
        NullLogger<ValidationBehavior<TestRequest, TestResponse>> logger = NullLogger<ValidationBehavior<TestRequest, TestResponse>>.Instance;
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators, logger);

        var request = new TestRequest("Valid Name");
        var expectedResponse = Result.Ok(new TestResponse("Success"));

        bool nextCalled = false;
        Task<Result<TestResponse>> Next()
        {
            nextCalled = true;
            return Task.FromResult(expectedResponse);
        }

        // Act
        Result<TestResponse> result = await behavior.HandleAsync(request, Next);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedResponse.Value.Value, result.Value.Value);
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailureWithValidationError_WhenValidationFails()
    {
        // Arrange
        TestRequestValidator[] validators = new[] { new TestRequestValidator() };
        NullLogger<ValidationBehavior<TestRequest, TestResponse>> logger = NullLogger<ValidationBehavior<TestRequest, TestResponse>>.Instance;
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators, logger);

        var request = new TestRequest(string.Empty); // Invalid request (empty name)

        bool nextCalled = false;
        Task<Result<TestResponse>> Next()
        {
            nextCalled = true;
            return Task.FromResult(Result.Ok(new TestResponse("Success")));
        }

        // Act
        Result<TestResponse> result = await behavior.HandleAsync(request, Next);

        // Assert
        Assert.True(result.IsFailure);
        Assert.False(nextCalled);

        ValidationError validationError = Assert.IsType<ValidationError>(result.Error);
        Assert.Single(validationError.Errors);
        Assert.Equal("Name", validationError.Errors[0].Code);
        Assert.Equal("Name cannot be empty", validationError.Errors[0].Description);
        Assert.Equal(ErrorType.Validation, validationError.Errors[0].Type);
    }
}
