// <copyright file="MapCompositingException.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

namespace WinterAdventurer.Library.Exceptions
{
    /// <summary>
    /// Exception raised when facility map image compositing fails.
    /// Inherits from PdfGenerationException to maintain exception hierarchy.
    /// </summary>
    public class MapCompositingException : PdfGenerationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MapCompositingException"/> class.
        /// </summary>
        public MapCompositingException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapCompositingException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public MapCompositingException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapCompositingException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public MapCompositingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
