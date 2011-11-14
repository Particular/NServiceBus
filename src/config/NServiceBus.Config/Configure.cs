using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using Common.Logging;
using NServiceBus.Config;
using NServiceBus.Config.ConfigurationSource;
using NServiceBus.ObjectBuilder;
using System.IO;
using System.Reflection;

namespace NServiceBus
{
    /// <summary>
    /// Central configuration entry point for NServiceBus.
    /// </summary>
    public class Configure
    {
        static Configure()
        {
            ConfigurationSource = new DefaultConfigurationSource();
        }

        /// <summary>
        /// Provides static access to the configuration object.
        /// </summary>
        public static Configure Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// Event raised when configuration is complete
        /// </summary>
        public static event Action ConfigurationComplete;

        /// <summary>
        /// Gets/sets the builder.
        /// Setting the builder should only be done by NServiceBus framework code.
        /// </summary>
        public IBuilder Builder { get; set; }

        private static bool initialized { get; set; }

        /// <summary>
        /// Gets/sets the configuration source to be used by NServiceBus.
        /// </summary>
        public static IConfigurationSource ConfigurationSource { get; set; }

        /// <summary>
        /// Sets the current configuration source
        /// </summary>
        /// <param name="configurationSource"></param>
        /// <returns></returns>
        public Configure CustomConfigurationSource(IConfigurationSource configurationSource)
        {
            ConfigurationSource = configurationSource;
            return this;
        }

        /// <summary>
        /// Gets/sets the object used to configure components.
        /// This object should eventually reference the same container as the Builder.
        /// </summary>
        public IConfigureComponents Configurer
        {
            get { return configurer; }
            set
            {
                configurer = value;
                ContainerHasBeenSet();
            }
        }

        private IConfigureComponents configurer;

        private void ContainerHasBeenSet()
        {
            TypesToScan
                .Where(t => t.GetInterfaces().Any(IsGenericConfigSource))
                .ToList().ForEach(t => configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));
        }

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

            return With(AppDomain.CurrentDomain.BaseDirectory);
        }

        /// <summary>
        /// Configures NServiceBus to scan for assemblies 
        /// in the relevant web directory instead of regular
        /// runtime directory.
        /// </summary>
        /// <returns></returns>
        public static Configure WithWeb()
        {
            return With(HttpRuntime.BinDirectory);
        }

        /// <summary>
        /// Configures NServiceBus to scan for assemblies
        /// in the given directory rather than the regular
        /// runtime directory.
        /// </summary>
        /// <param name="probeDirectory"></param>
        /// <returns></returns>
        public static Configure With(string probeDirectory)
        {
            return With(GetAssembliesInDirectory(probeDirectory));
        }

        /// <summary>
        /// Configures NServiceBus to use the types found in the given assemblies.
        /// </summary>
        /// <param name="assemblies"></param>
        /// <returns></returns>
        public static Configure With(IEnumerable<Assembly> assemblies)
        {
            return With(assemblies.ToArray());
        }

        /// <summary>
        /// Configures nServiceBus to scan the given assemblies only.
        /// </summary>
        /// <param name="assemblies"></param>
        /// <returns></returns>
        public static Configure With(params Assembly[] assemblies)
        {
            var types = new List<Type>();
            Array.ForEach(
                assemblies,
                a =>
                {
                    try
                    {
                        types.AddRange(a.GetTypes()
                            .Where(t => t.FullName == null || !defaultTypeExclusions.Any(exclusion => t.FullName.ToLower().StartsWith(exclusion))));
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        Logger.WarnFormat("Could not scan assembly: {0}. The reason is: {1}.", a.FullName, e.LoaderExceptions.First().Message, e);
                        return;//intentionally swallow exception
                    }
                });

            return With(types);
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

            TypesToScan = typesToScan;
            Logger.DebugFormat("Number of types to scan: {0}", TypesToScan.Count());
            return instance;
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
            Initialize();

            if (Configurer.HasComponent<IStartableBus>())
                return Builder.Build<IStartableBus>();

            return null;
        }

        /// <summary>
        /// Finalizes the configuration by invoking all initializers.
        /// </summary>
        public void Initialize()
        {
            if (initialized)
                return;

            if (Address.Local == null) // try to find a meaningful name
            {
                var trace = new StackTrace();
                StackFrame targetFrame = null;
                foreach (var f in trace.GetFrames())
                {
                    if (typeof(HttpApplication).IsAssignableFrom(f.GetMethod().DeclaringType))
                    {
                        targetFrame = f;
                        break;
                    }
                    var mi = f.GetMethod() as MethodInfo;
                    if (mi != null && mi.IsStatic && mi.ReturnType == typeof(void) && mi.Name == "Main" && mi.DeclaringType.Name == "Program")
                    {
                        targetFrame = f;
                        break;
                    }
                }

                if (targetFrame != null)
                {
                    string q = targetFrame.GetMethod().ReflectedType.Namespace;
                    Address.InitializeLocalAddress(q);
                }
            }

            TypesToScan.Where(t => typeof(IWantToRunWhenConfigurationIsComplete).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface))
                .ToList().ForEach(t => Configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));

            TypesToScan.Where(t => typeof(IWantToRunBeforeConfiguration).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface))
                .ToList().ForEach(t =>
                                      {
                                          var ini = (IWantToRunBeforeConfiguration)Activator.CreateInstance(t);
                                          ini.Init();
                                      });

            TypesToScan.Where(t => typeof(INeedInitialization).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface))
                .ToList().ForEach(t =>
                                      {
                                          var ini = (INeedInitialization)Activator.CreateInstance(t);
                                          ini.Init();
                                      });

            initialized = true;

            if (ConfigurationComplete != null)
                ConfigurationComplete();

            Builder.BuildAll<IWantToRunWhenConfigurationIsComplete>()
                .ToList().ForEach(o => o.Run());
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
        public static T GetConfigSection<T>() where T : class,new()
        {
            if (instance != null)
                if (instance.configurer != null)
                    if (instance.configurer.HasComponent<IProvideConfiguration<T>>())
                    {
                        var configSource = instance.Builder.Build<IProvideConfiguration<T>>();
                        if (configSource != null)
                            return configSource.GetConfiguration();
                    }

            return ConfigurationSource.GetConfiguration<T>();
        }

        /// <summary>
        /// Load and return all assemblies in the given directory except the given ones to exclude
        /// </summary>
        /// <param name="path"></param>
        /// <param name="assembliesToSkip"></param>
        /// <returns></returns>
        public static IEnumerable<Assembly> GetAssembliesInDirectory(string path, params string[] assembliesToSkip)
        {
            foreach (var a in GetAssembliesInDirectoryWithExtension(path, "*.exe", assembliesToSkip))
                yield return a;
            foreach (var a in GetAssembliesInDirectoryWithExtension(path, "*.dll", assembliesToSkip))
                yield return a;
        }

        private static IEnumerable<Assembly> GetAssembliesInDirectoryWithExtension(string path, string extension, params string[] assembliesToSkip)
        {
            var result = new List<Assembly>();

            foreach (FileInfo file in new DirectoryInfo(path).GetFiles(extension, SearchOption.AllDirectories))
            {
                try
                {
                    if (defaultAssemblyExclusions.Any(exclusion => file.Name.ToLower().StartsWith(exclusion)))
                        continue;

                    if (assembliesToSkip.Contains(file.Name, StringComparer.InvariantCultureIgnoreCase))
                        continue;

                    result.Add(Assembly.LoadFrom(file.FullName));
                }
                catch (BadImageFormatException bif)
                {
                    if (bif.FileName.ToLower().Contains("system.data.sqlite.dll"))
                        throw new BadImageFormatException(
                            "You've installed the wrong version of System.Data.SQLite.dll on this machine. If this machine is x86, this dll should be roughly 800KB. If this machine is x64, this dll should be roughly 1MB. You can find the x86 file under /binaries and the x64 version under /binaries/x64. *If you're running the samples, a quick fix would be to copy the file from /binaries/x64 over the file in /binaries - you should 'clean' your solution and rebuild after.",
                            bif.FileName, bif);

                    throw new InvalidOperationException(
                        "Could not load " + file.FullName +
                        ". Consider using 'Configure.With(AllAssemblies.Except(\"" + file.Name + "\"))' to tell NServiceBus not to load this file.",
                        bif);
                }
            }

            return result;
        }

        private static bool IsGenericConfigSource(Type t)
        {
            if (!t.IsGenericType)
                return false;

            var args = t.GetGenericArguments();
            if (args.Length != 1)
                return false;

            return typeof(IProvideConfiguration<>).MakeGenericType(args).IsAssignableFrom(t);
        }

        private static Configure instance;
        private static ILog Logger = LogManager.GetLogger("NServiceBus.Config");
        static readonly IEnumerable<string> defaultAssemblyExclusions = new[] { "system.", "nhibernate.", "log4net." };
        static readonly IEnumerable<string> defaultTypeExclusions = new[] { "raven." };
    }
}