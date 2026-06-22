using System.ComponentModel.DataAnnotations;
using WebAppExperimental266.Models.Main_Objects;

namespace WebAppExperimental266.Tests.Models
{
    public class ValidateStringNotWhitespaceTests
    {
        private class TestModel
        {
            [ValidateStringNotWhitespace(ErrorMessage = "Field cannot be whitespace")]
            public string? TestField { get; set; }
        }

        [Fact]
        public void IsValid_WithValidString_ReturnsTrue()
        {
            // Arrange
            var attribute = new ValidateStringNotWhitespace();
            var value = "Valid String";

            // Act
            var result = attribute.IsValid(value);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_WithNull_ReturnsTrue()
        {
            // Arrange
            var attribute = new ValidateStringNotWhitespace();

            // Act
            var result = attribute.IsValid(null);

            // Assert
            result.Should().BeTrue(); // Null is allowed, use [Required] for mandatory
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        [InlineData("\t")]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public void IsValid_WithWhitespace_ReturnsFalse(string whitespace)
        {
            // Arrange
            var attribute = new ValidateStringNotWhitespace();

            // Act
            var result = attribute.IsValid(whitespace);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithNonStringType_ReturnsTrue()
        {
            // Arrange
            var attribute = new ValidateStringNotWhitespace();
            var value = 123;

            // Act
            var result = attribute.IsValid(value);

            // Assert
            result.Should().BeTrue(); // Only validates strings
        }

        [Fact]
        public void Validation_WithModel_DetectsWhitespace()
        {
            // Arrange
            var model = new TestModel { TestField = "   " };
            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("Field cannot be whitespace");
        }

        [Fact]
        public void Validation_WithValidModel_Passes()
        {
            // Arrange
            var model = new TestModel { TestField = "Valid Value" };
            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }
    }
}
