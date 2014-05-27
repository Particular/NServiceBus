namespace NServiceBus.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Helpers;
    using Installation;
    using Logging;
    using Profiles;
    using Roles;
    using Settings;
    using Wcf;

    class GenericHost
    {
        public GenericHost(IConfigureThisEndpoint specifier, string[] args, List<Type> defaultProfiles, string endpointName, IEnumerable<string> scannableAssembliesFullName = null)
        {
            this.specifier = specifier;
            config = null;

            if (String.IsNullOrEmpty(endpointName))
            {
                endpointName = specifier.GetType().Namespace ?? specifier.GetType().Assembly.GetName().Name;
            }


            endpointNameToUse = endpointName;

            if (scannableAssembliesFullName == null || !scannableAssembliesFullName.Any())
            {
                var assemblyScanner = new AssemblyScanner();
                assemblyScanner.MustReferenceAtLeastOneAssembly.Add(typeof(IHandleMessages<>).Assembly);
                assembliesToScan = assemblyScanner
                    .GetScannableAssemblies()
                    .Assemblies;
            }
            else
            {
                assembliesToScan = scannableAssembliesFullName
                    .Select(Assembly.Load)
                    .ToList();
            }

            profileManager = new ProfileManager(assembliesToScan, args, defaultProfiles);
            ProfileActivator.ProfileManager = profileManager;

            wcfManager = new WcfManager();
            roleManager = new RoleManager(assembliesToScan);
        }

        /// <summary>
        ///     Creates and starts the bus as per the configuration
        /// </summary>
        public void Start()
        {
            try
            {
                PerformConfiguration();

                bus = Configure.Instance.CreateBus();
                if (bus != null && !SettingsHolder.Instance.Get<bool>("Endpoint.SendOnly"))
                {
                    bus.Start();
                }

                wcfManager.Startup(Configure.Instance);
            }
            catch (Exception ex)
            {
                LogManager.GetLogger<GenericHost>().Fatal("Exception when starting endpoint.", ex);
                throw;
            }
        }

        /// <summary>
        ///     Finalize
        /// </summary>
        public void Stop()
        {
            wcfManager.Shutdown();

            if (bus != null)
            {
                bus.Shutdown();
                bus.Dispose();

                bus = null;
            }
        }

        /// <summary>
        ///     When installing as windows service (/install), run infrastructure installers
        /// </summary>
        public void Install<TEnvironment>(string username) where TEnvironment : IEnvironment
        {
            PerformConfiguration();
            //HACK: to ensure the installer runner performs its installation
            Configure.Instance.Initialize();
        }

        void PerformConfiguration()
        {
            var loggingConfigurers = profileManager.GetLoggingConfigurer();
            foreach (var loggingConfigurer in loggingConfigurers)
            {
                loggingConfigurer.Configure(specifier);
            }

            var initialization = specifier as IWantCustomInitialization;
            if (initialization != null)
            {
                try
                {
                    initialization.Init();
                }
                catch (NullReferenceException ex)
                {
                    throw new NullReferenceException(
                        "NServiceBus has detected a null reference in your initialization code." +
                        " This could be due to trying to use NServiceBus.Configure before it was ready." +
                        " One possible solution is to inherit from IWantCustomInitialization in a different class" +
                        " than the one that inherits from IConfigureThisEndpoint, and put your code there.",
                        ex);
                }
            }

            if (config == null)
            {
                config = Configure.With(o =>
                {
                    o.EndpointName(endpointNameToUse);
                    o.AssembliesToScan(assembliesToScan);
                })
                .DefaultBuilder();
            }

            ValidateThatIWantCustomInitIsOnlyUsedOnTheEndpointConfig();

            roleManager.ConfigureBusForEndpoint(specifier);
        }

        void ValidateThatIWantCustomInitIsOnlyUsedOnTheEndpointConfig()
        {
            var problems = config.TypesToScan.Where(t => typeof(IWantCustomInitialization).IsAssignableFrom(t) && !t.IsInterface && !typeof(IConfigureThisEndpoint).IsAssignableFrom(t)).ToList();

            if (!problems.Any())
            {
                return;
            }

            throw new Exception("IWantCustomInitialization is only valid on the same class as ICOnfigureThisEndpoint. Please use INeedInitialization instead. Found types: " + string.Join(",",problems.Select(t=>t.FullName)));
        }

        readonly List<Assembly> assembliesToScan;
        readonly ProfileManager profileManager;
        readonly RoleManager roleManager;
        Configure config;
        readonly IConfigureThisEndpoint specifier;
        readonly WcfManager wcfManager;
        IStartableBus bus;
        string endpointNameToUse;
    }
}