using System;
using Common.Logging;

namespace NServiceBus.Host.Internal
{
    /// <summary>
    /// Implementation which hooks into TopShelf's Start/Stop lifecycle.
    /// </summary>
    public class GenericHost : MarshalByRefObject
    {
        /// <summary>
        /// Event raised when configuration is complete
        /// </summary>
        public static event EventHandler ConfigurationComplete;

        /// <summary>
        /// Does startup work.
        /// </summary>
        public void Start()
        {
            try
            {

                if (specifier is IWantCustomLogging)
                    (specifier as IWantCustomLogging).Init();
                else
                {
                    var loggingConfigurer = profileManager.GetLoggingConfigurer();
                    loggingConfigurer.Configure(specifier);
                }

                if (specifier is IWantCustomInitialization)
                    (specifier as IWantCustomInitialization).Init();
                else
                    Configure.With().SpringBuilder().XmlSerializer();

                if (Configure.Instance == null)
                    throw new ConfigurationException("Bus configuration has not been performed. Please call 'NServiceBus.Configure.With()' or one of its overloads.");
                if (Configure.Instance.Configurer == null || Configure.Instance.Builder == null)
                    throw new ConfigurationException("Container has not been configured for the bus. You may have forgotten to call 'NServiceBus.Configure.With().SpringBuilder()'.");

                RoleManager.ConfigureBusForEndpoint(specifier);

                configManager.ConfigureCustomInitAndStartup();

                profileManager.ActivateProfileHandlers();

                if (ConfigurationComplete != null)
                    ConfigurationComplete(this, null);

                var bus = Configure.Instance.Builder.Build<IStartableBus>();
                if (bus != null)
                    bus.Start();

                configManager.Startup();
            }
            catch (Exception ex)
            {
                //we log the error here in order to avoid issues with non serializable exceptions
                //going across the appdomain back to topshelf
                LogManager.GetLogger(typeof(GenericHost)).Fatal(ex);

                throw new Exception("Exception when starting endpoint, error has been logged. Reason: " + ex.Message);
            }
        }

        /// <summary>
        /// Does shutdown work.
        /// </summary>
        public void Stop()
        {
            configManager.Shutdown();
        }

        /// <summary>
        /// Accepts the type which will specify the users custom configuration.
        /// This type should implement <see cref="IConfigureThisEndpoint"/>.
        /// </summary>
        /// <param name="endpointType"></param>
        /// <param name="args"></param>
        public GenericHost(Type endpointType, string[] args)
        {
            specifier = (IConfigureThisEndpoint)Activator.CreateInstance(endpointType);

            Program.EndpointId = Program.GetEndpointId(specifier);

            var assembliesToScan = AssemblyScanner.GetScannableAssemblies();

            profileManager = new ProfileManager(assembliesToScan, specifier, args);
            configManager = new ConfigManager(assembliesToScan, specifier);
        }

        private readonly IConfigureThisEndpoint specifier;
        private readonly ProfileManager profileManager;
        private readonly ConfigManager configManager;
        private readonly ILog logger = LogManager.GetLogger(typeof(GenericHost));

    }
}