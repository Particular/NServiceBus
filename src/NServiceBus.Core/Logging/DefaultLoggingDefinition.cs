namespace NServiceBus.Logging
{
    using System;
    using System.IO;
    using System.Web;

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
            LoggingDirectory = FindDefaultLoggingDirectory();
            LogLevel = LogLevelReader.GetDefaultLogLevel();
        }
        /// <summary>
        /// The directory to log files to.
        /// </summary>
        public string LoggingDirectory { get; set; }
        /// <summary>
        /// <see cref="LoggingFactoryDefinition.GetLoggingFactory"/>.
        /// </summary>
        public override ILoggerFactory GetLoggingFactory()
        {
            return new DefaultLoggerFactory(LogLevel, LoggingDirectory);            
        }
        /// <summary>
        /// Controls the logging level.
        /// </summary>
        public LogLevel LogLevel { get; set; }

        static string FindDefaultLoggingDirectory()
        {
            //use appdata if it exists
            if (HttpContext.Current != null)
            {
                var appDataPath = HttpContext.Current.Server.MapPath("~/App_Data/");
                if (Directory.Exists(appDataPath))
                {
                    return appDataPath;
                }
            }
            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }
}