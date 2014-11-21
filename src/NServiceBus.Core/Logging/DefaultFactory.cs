namespace NServiceBus.Logging
{
    using System;
    using System.IO;
    using System.Web;
    using System.Web.Hosting;
    using IODirectory=System.IO.Directory;

    /// <summary>
    /// The default <see cref="LoggingFactoryDefinition"/>.
    /// </summary>
    public class DefaultFactory : LoggingFactoryDefinition
    {

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultFactory"/>.
        /// </summary>
        public DefaultFactory()
        {
            directory = new Lazy<string>(FindDefaultLoggingDirectory);
            level = new Lazy<LogLevel>(() => LogLevelReader.GetDefaultLogLevel());
        }

        /// <summary>
        /// <see cref="LoggingFactoryDefinition.GetLoggingFactory"/>.
        /// </summary>
        protected internal override ILoggerFactory GetLoggingFactory()
        {
            var loggerFactory = new DefaultLoggerFactory(level.Value, directory.Value);
            var message = string.Format("Logging to '{0}' with level {1}", directory, level);
            loggerFactory.Write(GetType().Name,LogLevel.Info,message);
            return loggerFactory;
        }

        Lazy<LogLevel> level;

        /// <summary>
        /// Controls the <see cref="LogLevel"/>.
        /// </summary>
        public void Level(LogLevel level)
        {
            this.level = new Lazy<LogLevel>(() => level);
        }

        Lazy<string> directory;

        /// <summary>
        /// The directory to log files to.
        /// </summary>
        public void Directory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentNullException("directory");
            }
            if (!IODirectory.Exists(directory))
            {
                var message = string.Format("Could not find logging directory: '{0}'", directory);
                throw new DirectoryNotFoundException(message);
            }
            this.directory = new Lazy<string>(() => directory);
        }

        internal static string FindDefaultLoggingDirectory()
        {
            if (HttpRuntime.AppDomainAppId == null)
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }

            return DeriveAppDataPath();
        }

        internal static string DeriveAppDataPath()
        {
            //we are in a website so attempt to MapPath
            var appDataPath = HostingEnvironment.MapPath("~/App_Data/");
            if (appDataPath != null)
            {
                if (IODirectory.Exists(appDataPath))
                {
                    return appDataPath;
                }
            }
            var error = "Detected running in a website but could not derive the path to '~/App_Data/'. Instead configure the logging directory using LogManager.Use<DefaultFactory>().Directory(\"pathToLoggingDirectory\");";
            throw new Exception(error);
        }
    }
}