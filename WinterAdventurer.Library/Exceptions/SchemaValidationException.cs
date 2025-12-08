using System;

namespace WinterAdventurer.Library.Exceptions
{
    /// <summary>
    /// Exception thrown when Excel file structure doesn't match the expected schema configuration.
    /// Indicates mismatches between the schema definition and actual file structure.
    /// </summary>
    public class SchemaValidationException : ExcelParsingException
    {
        /// <summary>
        /// Name of the schema that failed validation.
        /// </summary>
        public string? SchemaName { get; set; }

        /// <summary>
        /// Name of the missing sheet, if applicable.
        /// </summary>
        public string? MissingSheet { get; set; }

        /// <summary>
        /// List of sheet names that are available in the Excel file.
        /// </summary>
        public List<string> AvailableSheets { get; set; } = new List<string>();

        /// <summary>
        /// Initializes a new instance of the SchemaValidationException class.
        /// </summary>
        public SchemaValidationException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the SchemaValidationException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SchemaValidationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SchemaValidationException class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public SchemaValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
