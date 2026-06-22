using System.ComponentModel.DataAnnotations;


// REF:  https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-9.0#alternatives-to-built-in-attributes
// REF:  https://learn.microsoft.com/en-us/dotnet/api/system.string.isnullorwhitespace?view=net-9.0

namespace WebAppExperimental266.Models.Main_Objects
{
    /// <summary>
    /// Validate that a string does not hold whitespace
    /// </summary>
    public class ValidateStringNotWhitespace : ValidationAttribute
    {
        /// <summary>
        /// Apply String.IsNotNullOrWhitespace to a given value.
        /// </summary>
        public ValidateStringNotWhitespace()
        {

            const string defaultErrorMessage = "Error with string.";
            ErrorMessage ??= defaultErrorMessage;

        }

        /// <summary>
        /// Ensure the string is not null or whitespace
        /// </summary>
        /// <param name="value">string to be validated</param>
        /// <param name="validationContext"></param>
        /// <returns>ValidationResult object</returns>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // Null is allowed; use [Required] to enforce non-null.
            if (value == null)
            {
                return ValidationResult.Success;
            }

            if (value is string str && string.IsNullOrWhiteSpace(str))
            {
                var displayName = validationContext?.DisplayName ?? string.Empty;
                return new ValidationResult(FormatErrorMessage(displayName));
            }

            return ValidationResult.Success;
        }

    }
}
