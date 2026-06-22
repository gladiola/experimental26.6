using WebAppExperimental266.Models.User;

namespace WebAppExperimental266.Tests.Models
{
    public class UserClaimsTests
    {
        [Fact]
        public void Constructor_ShouldAllowPropertyInitialization()
        {
            // Act
            var userClaims = new UserClaims
            {
                Sid = "session-123",
                Oid = "object-456",
                Name = "Test User",
                Email = "test@example.com",
                Roles = new[] { "Admin", "User" }
            };

            // Assert
            userClaims.Sid.Should().Be("session-123");
            userClaims.Oid.Should().Be("object-456");
            userClaims.Name.Should().Be("Test User");
            userClaims.Email.Should().Be("test@example.com");
            userClaims.Roles.Should().HaveCount(2);
            userClaims.Roles.Should().Contain("Admin");
            userClaims.Roles.Should().Contain("User");
        }

        [Fact]
        public void Roles_CanBeEmptyArray()
        {
            // Act
            var userClaims = new UserClaims
            {
                Roles = Array.Empty<string>()
            };

            // Assert
            userClaims.Roles.Should().NotBeNull();
            userClaims.Roles.Should().BeEmpty();
        }

        [Fact]
        public void Roles_CanBeNull()
        {
            // Act
            var userClaims = new UserClaims
            {
                Roles = null!
            };

            // Assert
            userClaims.Roles.Should().BeNull();
        }

        [Fact]
        public void AllProperties_CanBeNull()
        {
            // Act
            var userClaims = new UserClaims
            {
                Sid = null,
                Oid = null,
                Name = null,
                Email = null,
                Roles = null!
            };

            // Assert
            userClaims.Sid.Should().BeNull();
            userClaims.Oid.Should().BeNull();
            userClaims.Name.Should().BeNull();
            userClaims.Email.Should().BeNull();
            userClaims.Roles.Should().BeNull();
        }

        [Fact]
        public void Roles_CanContainMultipleValues()
        {
            // Act
            var userClaims = new UserClaims
            {
                Roles = new[] { "Admin", "Editor", "Viewer", "Manager", "Contributor" }
            };

            // Assert
            userClaims.Roles.Should().HaveCount(5);
            userClaims.Roles.Should().ContainInOrder("Admin", "Editor", "Viewer", "Manager", "Contributor");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Properties_CanBeEmptyStrings(string emptyValue)
        {
            // Act
            var userClaims = new UserClaims
            {
                Sid = emptyValue,
                Oid = emptyValue,
                Name = emptyValue,
                Email = emptyValue
            };

            // Assert
            userClaims.Sid.Should().Be(emptyValue);
            userClaims.Oid.Should().Be(emptyValue);
            userClaims.Name.Should().Be(emptyValue);
            userClaims.Email.Should().Be(emptyValue);
        }
    }
}
