namespace NServiceBus.Testing
{
    using System;
    using System.IO;
    using Logging;

    /// <summary>
    /// Logger factory which allows to log to a text writer.
    /// </summary>
    public class TestingLoggerFactory : LoggingFactoryDefinition
    {
        /// <summary>
        /// Creates a new instance of a testing logger factory.
        /// </summary>
        public TestingLoggerFactory()
        {
            level = new Lazy<LogLevel>(() => LogLevel.Debug);
            writer = new Lazy<TextWriter>(() => TextWriter.Null);
        }

        /// <summary>
        /// Controls the <see cref="Logging.LogLevel" />.
        /// </summary>
        public void Level(LogLevel level)
        {
            this.level = new Lazy<LogLevel>(() => level);
        }

        /// <summary>
        /// Instructs the logger to write to the provided text writer.
        /// </summary>
        /// <param name="writer">The text writer to be used.</param>
        public void WriteTo(TextWriter writer)
        {
            this.writer = new Lazy<TextWriter>(() => writer);
        }

        /// <summary>
        /// Constructs an instance of <see cref="ILoggerFactory" /> for use by <see cref="LogManager.Use{T}" />.
        /// </summary>
        protected override ILoggerFactory GetLoggingFactory()
        {
            var loggerFactory = new DefaultTestingLoggerFactory(level.Value, writer.Value);
            var message = $"Logging to testing logger with level {level}";
            loggerFactory.Write(GetType().Name, LogLevel.Info, message);
            return loggerFactory;
        }

        Lazy<LogLevel> level;
        Lazy<TextWriter> writer;
    }
}