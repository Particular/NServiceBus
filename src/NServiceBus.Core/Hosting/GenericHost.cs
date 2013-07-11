namespace NServiceBus.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Configuration;
    using Helpers;
    using Installation;
    using Logging;
    using Profiles;
    using Roles;
    using Settings;
    using Utils;
    using Wcf;

    /// <summary>
    /// A generic host that can be used to provide hosting services in different environments
    /// </summary>
    public class GenericHost : IHost
    {
        /// <summary>
        /// Creates and starts the bus as per the configuration
        /// </summary>
        public void Start()
        {
            PerformConfiguration();

            bus = Configure.Instance.CreateBus();
            if (bus != null && !SettingsHolder.Get<bool>("Endpoint.SendOnly"))
            {
                bus.Start();
            }

            configManager.Startup();
            wcfManager.Startup();
        }

        /// <summary>
        /// Finalize
        /// </summary>
        public void Stop()
        {
            configManager.Shutdown();
            wcfManager.Shutdown();

            if (bus != null)
            {
                bus.Shutdown();
                bus.Dispose();

                bus = null;
            }
        }

        /// <summary>
        /// When installing as windows service (/install), run infrastructure installers
        /// </summary>
        /// <typeparam name="TEnvironment"></typeparam>
        public void Install<TEnvironment>(string username) where TEnvironment : IEnvironment
        {
            PerformConfiguration();
            Configure.Instance.ForInstallationOn<TEnvironment>(username).Install();
        }

        void PerformConfiguration()
        {
            if (specifier is IWantCustomLogging)
                (specifier as IWantCustomLogging).Init();
            else
            {
                var loggingConfigurers = profileManager.GetLoggingConfigurer();
                foreach (var loggingConfigurer in loggingConfigurers)
                {
                    loggingConfigurer.Configure(specifier);
                }
            }

            if (specifier is IWantCustomInitialization)
            {
                try
                {
                    if (specifier is IWantCustomLogging)
                    {
                        bool called = false;
                        //make sure we don't call the Init method again, unless there's an explicit impl
                        var initMap = specifier.GetType().GetInterfaceMap(typeof(IWantCustomInitialization));
                        foreach (var m in initMap.TargetMethods)
                            if (!m.IsPublic && m.Name == "NServiceBus.IWantCustomInitialization.Init")
                            {
                                (specifier as IWantCustomInitialization).Init();
                                called = true;
                            }

                        if (!called)
                        {
                            //call the regular Init method if IWantCustomLogging was an explicitly implemented method
                            var logMap = specifier.GetType().GetInterfaceMap(typeof(IWantCustomLogging));
                            foreach (var tm in logMap.TargetMethods)
                                if (!tm.IsPublic && tm.Name == "NServiceBus.IWantCustomLogging.Init")
                                    (specifier as IWantCustomInitialization).Init();
                        }
                    }
                    else
                        (specifier as IWantCustomInitialization).Init();
                }
                catch (NullReferenceException ex)
                {
                    throw new NullReferenceException("NServiceBus has detected a null reference in your initialization code." +
                                                     " This could be due to trying to use NServiceBus.Configure before it was ready." +
                                                     " One possible solution is to inherit from IWantCustomInitialization in a different class" +
                                                     " than the one that inherits from IConfigureThisEndpoint, and put your code there.",
                                                     ex);
                }
            }

            if (!Configure.WithHasBeenCalled())
                Configure.With();

            if (!Configure.BuilderIsConfigured())
                Configure.Instance.DefaultBuilder();

            roleManager.ConfigureBusForEndpoint(specifier);

            configManager.ConfigureCustomInitAndStartup();
        }


        /// <summary>
        /// Accepts the type which will specify the users custom configuration.
        /// This type should implement <see cref="IConfigureThisEndpoint"/>.
        /// </summary>
        /// <param name="specifier"></param>
        /// <param name="args"></param>
        /// <param name="defaultProfiles"></param>
        /// <param name="endpointName"></param>
        /// <param name="scannableAssembliesFullName">Assemblies full name that were scanned.</param>
        public GenericHost(IConfigureThisEndpoint specifier, string[] args, List<Type> defaultProfiles, string endpointName, IEnumerable<string> scannableAssembliesFullName = null)
        {
            this.specifier = specifier;

            if (String.IsNullOrEmpty(endpointName))
            {
                endpointName = specifier.GetType().Namespace ?? specifier.GetType().Assembly.GetName().Name;
            }

            Configure.GetEndpointNameAction = () => endpointName;
            Configure.DefineEndpointVersionRetriever = () => FileVersionRetriever.GetFileVersion(specifier.GetType());
            List<Assembly> assembliesToScan;

            if (scannableAssembliesFullName == null)
                assembliesToScan = AssemblyScanner.GetScannableAssemblies().Assemblies;
            else
                assembliesToScan = scannableAssembliesFullName.Select(Assembly.Load).ToList();

            profileManager = new ProfileManager(assembliesToScan, specifier, args, defaultProfiles);
            ProfileActivator.ProfileManager = profileManager;

            configManager = new ConfigManager(assembliesToScan, specifier);
            wcfManager = new WcfManager();
            roleManager = new RoleManager(assembliesToScan);
        }

        IStartableBus bus;
        readonly IConfigureThisEndpoint specifier;
        readonly ProfileManager profileManager;
        readonly ConfigManager configManager;
        readonly WcfManager wcfManager;
        readonly RoleManager roleManager;
    }
}