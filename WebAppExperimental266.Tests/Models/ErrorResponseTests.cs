using WebAppExperimental266.Models.Main_Objects;

namespace WebAppExperimental266.Tests.Models
{
    public class ErrorResponseTests
    {
        [Fact]
        public void Constructor_ShouldSetProperties()
        {
            // Arrange
            var errorMessage = "Test error message";
            var errorNumber = 400;

            // Act
            var errorResponse = new ErrorResponse
            {
                ErrorMessage = errorMessage,
                ErrorNumber = errorNumber
            };

            // Assert
            errorResponse.ErrorMessage.Should().Be(errorMessage);
            errorResponse.ErrorNumber.Should().Be(errorNumber);
        }

        [Fact]
        public void DefaultConstructor_ShouldCreateEmptyObject()
        {
            // Act
            var errorResponse = new ErrorResponse();

            // Assert
            errorResponse.ErrorMessage.Should().BeNull();
            errorResponse.ErrorNumber.Should().Be(0);
        }

        [Theory]
        [InlineData(400, "Bad Request")]
        [InlineData(401, "Unauthorized")]
        [InlineData(403, "Forbidden")]
        [InlineData(404, "Not Found")]
        [InlineData(500, "Internal Server Error")]
        public void ErrorNumber_ShouldAcceptCommonHttpCodes(int errorNumber, string message)
        {
            // Act
            var errorResponse = new ErrorResponse
            {
                ErrorNumber = errorNumber,
                ErrorMessage = message
            };

            // Assert
            errorResponse.ErrorNumber.Should().Be(errorNumber);
            errorResponse.ErrorMessage.Should().Be(message);
        }

        [Fact]
        public void ErrorMessage_CanBeEmpty()
        {
            // Act
            var errorResponse = new ErrorResponse
            {
                ErrorMessage = string.Empty,
                ErrorNumber = 400
            };

            // Assert
            errorResponse.ErrorMessage.Should().BeEmpty();
        }
    }
}
