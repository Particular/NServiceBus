namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using Hosting.Helpers;
    using Hosting.Profiles;
    using Hosting.Wcf;
    using Logging;
    using NServiceBus.Unicast;

    class GenericHost
    {
        public GenericHost(IConfigureThisEndpoint specifier, string[] args, List<Type> defaultProfiles, string endpointName, IEnumerable<string> scannableAssembliesFullName = null)
        {
            this.specifier = specifier;
         
            if (String.IsNullOrEmpty(endpointName))
            {
                endpointName = specifier.GetType().Namespace ?? specifier.GetType().Assembly.GetName().Name;
            }


            endpointNameToUse = endpointName;
            endpointVersionToUse = FileVersionRetriever.GetFileVersion(specifier.GetType());

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

            wcfManager = new WcfManager();
        }

        /// <summary>
        ///     Creates and starts the bus as per the configuration
        /// </summary>
        public void Start()
        {
            try
            {
                PerformConfiguration();

                if (bus != null && !bus.Settings.Get<bool>("Endpoint.SendOnly"))
                {
                    bus.Start();
                }

                wcfManager.Startup(bus);
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
        public void Install(string username)
        {
            PerformConfiguration(builder => builder.EnableInstallers(username));
          
            bus.Builder.Dispose();
        }

        void PerformConfiguration(Action<BusConfiguration> moreConfiguration = null)
        {
            var loggingConfigurers = profileManager.GetLoggingConfigurer();
            foreach (var loggingConfigurer in loggingConfigurers)
            {
                loggingConfigurer.Configure(specifier);
            }

            var builder = new BusConfiguration();

            builder.EndpointName(endpointNameToUse);
            builder.EndpointVersion(endpointVersionToUse);
            builder.AssembliesToScan(assembliesToScan);
            builder.DefineCriticalErrorAction(OnCriticalError);

            if (moreConfiguration != null)
            {
                moreConfiguration(builder);
            }

            specifier.Customize(builder);
            RoleManager.TweakConfigurationBuilder(specifier, builder);
            profileManager.ActivateProfileHandlers(builder);

            bus = (UnicastBus)Configure.With(builder);
        }

        // Windows hosting behavior when critical error occurs is suicide.
        void OnCriticalError(string errorMessage, Exception exception)
        {
            if (Environment.UserInteractive)
            {
                Thread.Sleep(10000); // so that user can see on their screen the problem
            }
            
            Environment.FailFast(String.Format("The following critical error was encountered by NServiceBus:\n{0}\nNServiceBus is shutting down.", errorMessage), exception);
        }
        List<Assembly> assembliesToScan;
        ProfileManager profileManager;
        IConfigureThisEndpoint specifier;
        WcfManager wcfManager;
        UnicastBus bus;
        string endpointNameToUse;
        string endpointVersionToUse;
    }
}