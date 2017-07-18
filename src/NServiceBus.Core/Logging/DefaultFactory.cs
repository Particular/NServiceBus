namespace NServiceBus.Logging
{
    using System;
    using System.IO;
    using System.Reflection;
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
            directory = new Lazy<string>(FindDefaultLoggingDirectory);
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

        internal static string FindDefaultLoggingDirectory()
        {
            Assembly systemWebAssembly = null;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "System.Web")
                {
                    systemWebAssembly = assembly;
                    break;
                }
            }

            var httpRuntime = systemWebAssembly?.GetType("System.Web.HttpRuntime");
            var appDomainAppId = httpRuntime?.GetProperty("AppDomainAppId", BindingFlags.Public | BindingFlags.Static);
            var result = appDomainAppId?.GetValue(null);

            if (result == null)
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }

            return DeriveAppDataPath(systemWebAssembly);
        }

        static string DeriveAppDataPath(Assembly systemWebAssembly)
        {
            var appDataPath = TryMapPath(systemWebAssembly);

            if (appDataPath == null)
            {
                throw new Exception(GetMapPathError("Failed since MapPath returned null"));
            }

            if (IODirectory.Exists(appDataPath))
            {
                return appDataPath;
            }

            throw new Exception(GetMapPathError($"Failed since path returned ({appDataPath}) does not exist. Ensure this directory is created and restart the endpoint."));
        }

        static string TryMapPath(Assembly systemWebAssembly)
        {
            try
            {
                var hostingEnvironment = systemWebAssembly?.GetType("System.Web.Hosting.HostingEnvironment");
                var mapPath = hostingEnvironment?.GetMethod("MapPath", BindingFlags.Static | BindingFlags.Public);
                var result = mapPath?.Invoke(null, new[] { "~/App_Data/" }) as string;

                return result;
            }
            catch (Exception exception)
            {
                throw new Exception(GetMapPathError("Failed since MapPath threw an exception"), exception);
            }
        }

        static string GetMapPathError(string reason)
        {
            return $"Detected running in a website and attempted to use HostingEnvironment.MapPath(\"~/App_Data/\") to derive the logging path. {reason}. To avoid using HostingEnvironment.MapPath to derive the logging directory you can instead configure it to a specific path using LogManager.Use<DefaultFactory>().Directory(\"pathToLoggingDirectory\");";
        }

        Lazy<string> directory;

        Lazy<LogLevel> level;
    }
}