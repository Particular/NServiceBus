using System.IO;
using System.Linq;
using System.Reflection;
using System;

namespace NServiceBus.Logging
{

    /// <summary>
    /// 
    /// </summary>
    public static class LogManager
    {
        private static ILoggerFactory _loggerFactory;


        static LogManager()
        {
            //TODO: beter way to get location?
            var currentDirectory = CurrentDirectory();
            var loggingAssembly = Directory.EnumerateFiles(currentDirectory, "NServiceBus.Logging.*").FirstOrDefault();
            if (loggingAssembly == null)
            {
                var message = string.Format("Could not find a logging extension library (NServiceBus.Logging.*) in the current directory ({0}). The easiest way to find one is to get it from nuget http://nuget.org/packages?q=NServiceBus.Logging", currentDirectory);
                throw new Exception(message);
            }
            var expectedTypeName = Path.GetFileNameWithoutExtension(loggingAssembly) + ".LoggerFactory";
            var loggerFactoryType = Assembly.LoadFrom(loggingAssembly).GetType(expectedTypeName,false);
            if (loggerFactoryType == null)
            {
                throw new Exception(string.Format("Could not find a type named '{0}' in the assembly '{1}'.", expectedTypeName, loggingAssembly));
            }
            var instance = Activator.CreateInstance(loggerFactoryType);
            _loggerFactory = instance as ILoggerFactory;
            if (_loggerFactory == null)
            {
                throw new Exception(string.Format("Could not cast '{0}' to ILoggerFactory.", expectedTypeName));
            }
        }

        static string CurrentDirectory()
        {
            var assembly = typeof(LogManager).Assembly;
            var uri = new UriBuilder(assembly.CodeBase);
            var path = Uri.UnescapeDataString(uri.Path);

            return Path.GetDirectoryName(path);
        }
        /// <summary>
        /// 
        /// </summary>
        public static ILoggerFactory LoggerFactory
        {
            get { return _loggerFactory; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _loggerFactory = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ILog GetLogger(Type type)
        {
            return LoggerFactory.GetLogger(type);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ILog GetLogger(string name)
        {
            return LoggerFactory.GetLogger(name);
        }
    }
}