// <copyright file="MissingColumnException.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

namespace WinterAdventurer.Library.Exceptions
{
    /// <summary>
    /// Exception thrown when a required column is not found in an Excel sheet.
    /// Provides the expected pattern and list of available columns to help diagnose the issue.
    /// </summary>
    public class MissingColumnException : ExcelParsingException
    {
        /// <summary>
        /// Gets or sets expected pattern or exact name for the column.
        /// </summary>
        public string ExpectedPattern { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets list of column names that are available in the sheet.
        /// </summary>
        public List<string> AvailableColumns { get; set; } = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingColumnException"/> class.
        /// </summary>
        public MissingColumnException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingColumnException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public MissingColumnException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingColumnException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public MissingColumnException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
