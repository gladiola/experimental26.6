using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Tests.Services
{
    public class AwsDynamoDbServiceTests
    {
        private readonly Mock<IAmazonDynamoDB> _mockClient;
        private readonly AwsDynamoDbService _service;

        public AwsDynamoDbServiceTests()
        {
            _mockClient = new Mock<IAmazonDynamoDB>();
            _service = new AwsDynamoDbService(_mockClient.Object, "test-table");
        }

        [Fact]
        public void Constructor_ShouldAcceptClientAndTableName()
        {
            _service.Should().NotBeNull();
        }

        [Fact]
        public void GetTableName_ShouldReturnConfiguredTableName()
        {
            _service.GetTableName().Should().Be("test-table");
        }

        [Fact]
        public async Task GetTableAsync_ShouldCallDescribeTable()
        {
            // Arrange
            var tableDescription = new TableDescription
            {
                TableName = "test-table",
                TableStatus = TableStatus.ACTIVE
            };
            _mockClient
                .Setup(c => c.DescribeTableAsync(
                    It.Is<DescribeTableRequest>(r => r.TableName == "test-table"),
                    default))
                .ReturnsAsync(new DescribeTableResponse { Table = tableDescription });

            // Act
            var result = await _service.GetTableAsync();

            // Assert
            result.Should().NotBeNull();
            result.TableName.Should().Be("test-table");
            _mockClient.Verify(
                c => c.DescribeTableAsync(
                    It.Is<DescribeTableRequest>(r => r.TableName == "test-table"),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task GetTableAsync_WhenDescribeTableFails_ShouldPropagateException()
        {
            // Arrange
            _mockClient
                .Setup(c => c.DescribeTableAsync(It.IsAny<DescribeTableRequest>(), default))
                .ThrowsAsync(new ResourceNotFoundException("Table not found"));

            // Act
            Func<Task> act = async () => await _service.GetTableAsync();

            // Assert
            await act.Should().ThrowAsync<ResourceNotFoundException>();
        }

        [Theory]
        [InlineData("orders")]
        [InlineData("users")]
        [InlineData("sessions")]
        public void GetTableName_WithDifferentTableNames_ReturnsCorrectName(string tableName)
        {
            var svc = new AwsDynamoDbService(_mockClient.Object, tableName);
            svc.GetTableName().Should().Be(tableName);
        }
    }
}
