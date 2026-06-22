using WebAppExperimental266.Models.Main_Objects;

namespace WebAppExperimental266.Tests.Models
{
    public class DataProcessingStatusTests
    {
        [Fact]
        public void AllStatuses_ShouldHaveUniqueValues()
        {
            // Arrange
            var allStatuses = Enum.GetValues<DataProcessingStatus>();

            // Act
            var uniqueValues = allStatuses.Select(s => (int)s).Distinct();

            // Assert
            uniqueValues.Should().HaveCount(allStatuses.Length);
        }

        [Fact]
        public void AllStatuses_ShouldHaveUniqueNames()
        {
            // Arrange
            var allStatuses = Enum.GetValues<DataProcessingStatus>();

            // Act
            var uniqueNames = allStatuses.Select(s => s.ToString()).Distinct();

            // Assert
            uniqueNames.Should().HaveCount(allStatuses.Length);
        }

        [Fact]
        public void Success_ShouldExist()
        {
            // Assert
            DataProcessingStatus.Success.ToString().Should().Be("Success");
        }

        [Fact]
        public void Failure_ShouldExist()
        {
            // Assert
            DataProcessingStatus.Failure.ToString().Should().Be("Failure");
        }

        [Fact]
        public void Info_ShouldExist()
        {
            // Assert
            DataProcessingStatus.Info.ToString().Should().Be("Info");
        }

        [Fact]
        public void Exception_ShouldExist()
        {
            // Assert
            DataProcessingStatus.Exception.ToString().Should().Be("Exception");
        }

        [Fact]
        public void ToString_ShouldReturnName()
        {
            // Act & Assert
            DataProcessingStatus.Success.ToString().Should().Be("Success");
            DataProcessingStatus.Failure.ToString().Should().Be("Failure");
        }

        [Fact]
        public void Equals_SameStatus_ShouldReturnTrue()
        {
            // Act
            var result = DataProcessingStatus.Success.Equals(DataProcessingStatus.Success);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentStatus_ShouldReturnFalse()
        {
            // Act
            var result = DataProcessingStatus.Success.Equals(DataProcessingStatus.Failure);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("not a status")]
        public void Equals_InvalidObject_ShouldReturnFalse(object? obj)
        {
            // Act
            var result = DataProcessingStatus.Success.Equals(obj);

            // Assert
            result.Should().BeFalse();
        }
    }
}
