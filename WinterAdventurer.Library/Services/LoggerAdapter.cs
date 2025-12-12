// <copyright file="LoggerAdapter.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using Microsoft.Extensions.Logging;

namespace WinterAdventurer.Library.Services
{
    /// <summary>
    /// Adapter that wraps an ILogger to provide an ILogger&lt;T&gt; interface.
    /// Used to share a single logger instance across multiple generic logger interfaces.
    /// </summary>
    /// <typeparam name="T">The type parameter for the generic logger interface.</typeparam>
    internal sealed class LoggerAdapter<T> : ILogger<T>
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerAdapter{T}"/> class.
        /// </summary>
        /// <param name="logger">The underlying logger to delegate to.</param>
        public LoggerAdapter(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return _logger.BeginScope(state);
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
