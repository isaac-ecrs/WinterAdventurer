using System;

namespace WinterAdventurer.Library.Exceptions
{
    /// <summary>
    /// Base exception for PDF generation errors.
    /// Provides context about which section of the PDF failed to generate.
    /// </summary>
    public class PdfGenerationException : Exception
    {
        /// <summary>
        /// Section of the PDF where the error occurred (e.g., "Roster", "Schedule", "MasterSchedule").
        /// </summary>
        public string Section { get; set; } = string.Empty;

        /// <summary>
        /// Initializes a new instance of the PdfGenerationException class.
        /// </summary>
        public PdfGenerationException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the PdfGenerationException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public PdfGenerationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PdfGenerationException class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public PdfGenerationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
