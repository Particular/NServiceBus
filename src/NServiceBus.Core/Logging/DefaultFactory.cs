namespace NServiceBus.Logging
{
    using System;
    using System.IO;
    using IODirectory = System.IO.Directory;

    /// <summary>
    /// The default <see cref="LoggingFactoryDefinition" />.
    /// </summary>
    public class DefaultFactory : LoggingFactoryDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DefaultFactory" />.
        /// </summary>
        public DefaultFactory()
        {
            directory = new Lazy<string>(Host.GetOutputDirectory);
            level = new Lazy<LogLevel>(() => LogLevel.Info);
        }

        /// <summary>
        /// <see cref="LoggingFactoryDefinition.GetLoggingFactory" />.
        /// </summary>
        protected internal override ILoggerFactory GetLoggingFactory()
        {
            var loggerFactory = new DefaultLoggerFactory(level.Value, directory.Value);
            var message = $"Logging to '{directory}' with level {level}";
            loggerFactory.Write(GetType().Name, LogLevel.Info, message);

            return loggerFactory;
        }

        /// <summary>
        /// Controls the <see cref="LogLevel" />.
        /// </summary>
        public void Level(LogLevel level)
        {
            this.level = new Lazy<LogLevel>(() => level);
        }

        /// <summary>
        /// The directory to log files to.
        /// </summary>
        public void Directory(string directory)
        {
            Guard.AgainstNullAndEmpty(nameof(directory), directory);

            if (!IODirectory.Exists(directory))
            {
                var message = $"Could not find logging directory: '{directory}'";
                throw new DirectoryNotFoundException(message);
            }

            this.directory = new Lazy<string>(() => directory);
        }

        Lazy<string> directory;
        Lazy<LogLevel> level;
    }
}