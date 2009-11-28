using System;
using System.Collections.Generic;
using System.Web;
using NServiceBus.Config.ConfigurationSource;
using NServiceBus.ObjectBuilder;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Common.Logging;

namespace NServiceBus
{
    /// <summary>
    /// Central configuration entry point for NServiceBus.
    /// </summary>
    public class Configure
    {
        /// <summary>
        /// Provides static access to the configuration object.
        /// </summary>
        public static Configure Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// Gets/sets the builder.
        /// Setting the builder should only be done by NServiceBus framework code.
        /// </summary>
        public IBuilder Builder { get; set; }

        /// <summary>
        /// Provides access to the configuration source.
        /// </summary>
        protected IConfigurationSource ConfigSource { get; set; }

        /// <summary>
        /// Gets the current configuration source
        /// </summary>
        public static IConfigurationSource ConfigurationSource
        {
            get { return instance.ConfigSource; }
        }

        /// <summary>
        /// Sets the current configuration source
        /// </summary>
        /// <param name="configurationSource"></param>
        /// <returns></returns>
        public Configure CustomConfigurationSource(IConfigurationSource configurationSource)
        {
            ConfigSource = configurationSource;
            return this;
        }

        /// <summary>
        /// Gets/sets the object used to configure components.
        /// This object should eventually reference the same container as the Builder.
        /// </summary>
        public IConfigureComponents Configurer { get; set; }

        /// <summary>
        /// Protected constructor to enable creation only via the With method.
        /// </summary>
        protected Configure()
        {
        }

        /// <summary>
        /// Creates a new configuration object scanning assemblies
        /// in the regular runtime directory.
        /// </summary>
        /// <returns></returns>
        public static Configure With()
        {
            if (HttpContext.Current != null)
                throw new InvalidOperationException("NServiceBus has detected that you're running in the context of a web application. The method 'NServiceBus.Configure.With()' is not recommended for web scenarios. Use 'NServiceBus.Configure.WithWeb()' instead, or consider explicitly passing in the assemblies you want to be scanned to one of the overloads to the 'With' method.");

            return With(new List<Type>(GetTypesInDirectory(AppDomain.CurrentDomain.BaseDirectory)));
        }

        /// <summary>
        /// Configures nServiceBus to scan for assemblies 
        /// in the relevant web directory instead of regular
        /// runtime directory.
        /// </summary>
        /// <returns></returns>
        public static Configure WithWeb()
        {
            return With(new List<Type>(GetTypesInDirectory(AppDomain.CurrentDomain.DynamicDirectory)));
        }

        /// <summary>
        /// Configures nServiceBus to scan for assemblies
        /// in the given directory rather than the regular
        /// runtime directory.
        /// </summary>
        /// <param name="probeDirectory"></param>
        /// <returns></returns>
        public static Configure With(string probeDirectory)
        {
            return With(new List<Type>(GetTypesInDirectory(probeDirectory)));
        }

        /// <summary>
        /// Configures nServiceBus to scan the given types.
        /// </summary>
        /// <param name="typesToScan"></param>
        /// <returns></returns>
        public static Configure With(IEnumerable<Type> typesToScan)
        {
            if (instance == null)
                instance = new Configure();

            instance.ConfigSource = new DefaultConfigurationSource();

            TypesToScan = typesToScan;

            return instance;
        }

        /// <summary>
        /// Configures nServiceBus to scan the given assemblies only.
        /// </summary>
        /// <param name="assemblies"></param>
        /// <returns></returns>
        public static Configure With(params Assembly[] assemblies)
        {
            var types = new List<Type>();
            new List<Assembly>(assemblies).ForEach(a => { foreach (Type t in a.GetTypes()) types.Add(t); });

            return With(types);
        }

        /// <summary>
        /// Run a custom action at configuration time - useful for performing additional configuration not exposed by the fluent interface.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public Configure RunCustomAction(Action action)
        {
            action();

            return this;
        }

        /// <summary>
        /// Provides an instance to a startable bus.
        /// </summary>
        /// <returns></returns>
        public IStartableBus CreateBus()
        {
            return Builder.Build<IStartableBus>();
        }

        /// <summary>
        /// Returns types in assemblies found in the current directory.
        /// </summary>
        public static IEnumerable<Type> TypesToScan { get; private set; }

        /// <summary>
        /// Returns the requested config section using the current configuration source
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetConfigSection<T>() where T : class
        {
            return ConfigurationSource.GetConfiguration<T>();
        }

        private static IEnumerable<Type> GetTypesInDirectory(string path)
        {
            foreach (Type t in GetTypesInDirectoryWithExtension(path, "*.exe"))
                yield return t;
            foreach (Type t in GetTypesInDirectoryWithExtension(path, "*.dll"))
                yield return t;
        }

        private static IEnumerable<Type> GetTypesInDirectoryWithExtension(string path, string extension)
        {
            var result = new List<Type>();
            try
            {
                foreach (FileInfo file in new DirectoryInfo(path).GetFiles(extension, SearchOption.AllDirectories))
                {
                    Type[] types;
                    try
                    {
                        Assembly a = Assembly.LoadFrom(file.FullName);

                        types = a.GetTypes();
                    }
                    catch (ReflectionTypeLoadException err)
                    {
                        foreach (var loaderException in err.LoaderExceptions)
                        {
                            logger.Error("Problem with loading " + file.FullName, loaderException);
                        }

                        throw;
                    }

                    result.AddRange(types);
                }
            }
            catch (BadImageFormatException bif)
            {
                if (bif.FileName.ToLower().Contains("system.data.sqlite.dll"))
                    throw new BadImageFormatException("You've installed the wrong version of System.Data.SQLite.dll on this machine. If this machine is x86, this dll should be roughly 800KB. If this machine is x64, this dll should be roughly 1MB. You can find the x86 file under /binaries and the x64 version under /binaries/x64. *If you're running the samples, a quick fix would be to copy the file from /binaries/x64 over the file in /binaries - you should 'clean' your solution and rebuild after.", bif.FileName, bif);

                throw;
            }

            return result;
        }

        private static Configure instance;
        private static ILog logger = LogManager.GetLogger("NServiceBus.Config");
    }
}
