namespace Skugga.Core.Exceptions
{
    /// <summary>
    /// Represents a validation error for a specific field.
    /// Used with BadRequestException for structured error responses.
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Gets the name of the field that failed validation.
        /// </summary>
        public string Field { get; }

        /// <summary>
        /// Gets the validation error message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the error code (optional, e.g., "required", "min_length", "invalid_format").
        /// </summary>
        public string? Code { get; }

        /// <summary>
        /// Initializes a new instance of ValidationError.
        /// </summary>
        /// <param name="field">The field name.</param>
        /// <param name="message">The error message.</param>
        public ValidationError(string field, string message)
        {
            Field = field;
            Message = message;
        }

        /// <summary>
        /// Initializes a new instance of ValidationError with an error code.
        /// </summary>
        /// <param name="field">The field name.</param>
        /// <param name="message">The error message.</param>
        /// <param name="code">The error code.</param>
        public ValidationError(string field, string message, string code)
        {
            Field = field;
            Message = message;
            Code = code;
        }

        /// <summary>
        /// Returns a string representation of the validation error.
        /// </summary>
        public override string ToString()
        {
            return Code != null
                ? $"{Field}: {Message} (code: {Code})"
                : $"{Field}: {Message}";
        }
    }
}
