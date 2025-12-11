// <copyright file="MissingResourceException.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

namespace WinterAdventurer.Library.Exceptions
{
    /// <summary>
    /// Exception thrown when an embedded resource (font, image, schema) cannot be found or loaded.
    /// Indicates missing resources that are required for PDF generation.
    /// </summary>
    public class MissingResourceException : PdfGenerationException
    {
        /// <summary>
        /// Gets or sets name of the missing resource.
        /// </summary>
        public string ResourceName { get; set; } = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingResourceException"/> class.
        /// </summary>
        public MissingResourceException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingResourceException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public MissingResourceException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingResourceException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public MissingResourceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
