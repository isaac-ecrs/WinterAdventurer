// <copyright file="InvalidWorkshopFormatException.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

namespace WinterAdventurer.Library.Exceptions
{
    /// <summary>
    /// Exception thrown when a workshop cell value doesn't match the expected format.
    /// Expected format is typically "WorkshopName (LeaderName)".
    /// </summary>
    public class InvalidWorkshopFormatException : ExcelParsingException
    {
        /// <summary>
        /// Gets or sets the actual cell value that failed to parse.
        /// </summary>
        public string CellValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expected format for workshop cells.
        /// </summary>
        public string ExpectedFormat { get; set; } = "WorkshopName (LeaderName)";

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidWorkshopFormatException"/> class.
        /// </summary>
        public InvalidWorkshopFormatException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidWorkshopFormatException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public InvalidWorkshopFormatException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidWorkshopFormatException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public InvalidWorkshopFormatException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
