using System;
using System.Collections.Generic;

namespace WinterAdventurer.Library.Exceptions
{
    /// <summary>
    /// Exception thrown when a required Excel sheet is not found in the workbook.
    /// Provides the list of available sheets to help diagnose the issue.
    /// </summary>
    public class MissingSheetException : ExcelParsingException
    {
        /// <summary>
        /// List of sheet names that are available in the Excel file.
        /// </summary>
        public List<string> AvailableSheets { get; set; } = new List<string>();

        /// <summary>
        /// Initializes a new instance of the MissingSheetException class.
        /// </summary>
        public MissingSheetException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the MissingSheetException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public MissingSheetException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MissingSheetException class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public MissingSheetException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
