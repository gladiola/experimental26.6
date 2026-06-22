using WebAppExperimental266.Models;

namespace WebAppExperimental266.Tests.Models
{
    public class ErrorViewModelTests
    {
        [Fact]
        public void RequestId_CanBeSet()
        {
            // Arrange
            var model = new ErrorViewModel();
            var requestId = "test-request-123";

            // Act
            model.RequestId = requestId;

            // Assert
            model.RequestId.Should().Be(requestId);
        }

        [Fact]
        public void ShowRequestId_ReturnsFalse_WhenRequestIdIsNull()
        {
            // Arrange
            var model = new ErrorViewModel { RequestId = null };

            // Act
            var result = model.ShowRequestId;

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ShowRequestId_ReturnsFalse_WhenRequestIdIsEmpty()
        {
            // Arrange
            var model = new ErrorViewModel { RequestId = string.Empty };

            // Act
            var result = model.ShowRequestId;

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ShowRequestId_ReturnsTrue_WhenRequestIdHasValue()
        {
            // Arrange
            var model = new ErrorViewModel { RequestId = "some-request-id" };

            // Act
            var result = model.ShowRequestId;

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void ShowRequestId_ReturnsFalse_ForEmptyOrWhitespace(string? requestId)
        {
            // Arrange
            var model = new ErrorViewModel { RequestId = requestId };

            // Act
            var result = model.ShowRequestId;

            // Assert
            result.Should().BeFalse();
        }
    }
}
