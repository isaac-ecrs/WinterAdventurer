// <copyright file="ExcelParsingException.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

namespace WinterAdventurer.Library.Exceptions
{
    /// <summary>
    /// Base exception for all Excel parsing errors.
    /// Provides context about the sheet, row, and column where parsing failed.
    /// </summary>
    public class ExcelParsingException : Exception
    {
        /// <summary>
        /// Gets or sets name of the Excel sheet where the error occurred.
        /// </summary>
        public string? SheetName { get; set; }

        /// <summary>
        /// Gets or sets row number where the error occurred (1-indexed to match Excel).
        /// </summary>
        public int? RowNumber { get; set; }

        /// <summary>
        /// Gets or sets column name where the error occurred.
        /// </summary>
        public string? ColumnName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelParsingException"/> class.
        /// </summary>
        public ExcelParsingException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelParsingException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ExcelParsingException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelParsingException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ExcelParsingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
