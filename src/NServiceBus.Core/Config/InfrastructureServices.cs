namespace NServiceBus.Config
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Text;
    using NServiceBus.Logging;
    using Settings;

    /// <summary>
    /// Class used to control the various infrastructure services required by NServiceBus
    /// </summary>
    public class InfrastructureServices
    {
        public static IEnumerable<ServiceStatus> CurrentStatus
        {
            get { return knownServices.Values; }
        }

        /// <summary>
        /// Enables the given infrastructure service by registering it in the container
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void Enable<T>()
        {
            var serviceType = typeof(T);

            if (Configure.Instance.Configurer.HasComponent<T>())
            {
                Logger.InfoFormat("Infrastructure service {0} was found in the container and will be used instead of the default", serviceType.FullName);
                SetStatusToEnabled(serviceType);

                //todo: We should guide users to register their infrastructure overrides in a more explicit way in the future
                return;
            }

            if (!SettingsHolder.HasSetting<T>())
            {
                throw new ConfigurationErrorsException(string.Format("No explicit settings or default found for service {0}, please configure one explicity", serviceType.FullName));
            }

            var configAction = SettingsHolder.Get<Action>(serviceType.FullName);

            configAction();

            SetStatusToEnabled(serviceType);

            Logger.DebugFormat("Infrastructure service {0} has been configured", serviceType.FullName);
        }

    

        /// <summary>
        /// Set the default for the infrastructure service to the action passed in.
        /// If the service is enabled and no explicit override is found this action will be used to configure the service.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configAction"></param>
        public static void SetDefaultFor<T>(Action configAction)
        {
            var serviceType = typeof(T);

            SettingsHolder.SetDefault<T>(configAction);
            SetStatusToDisabled(serviceType);
            Logger.DebugFormat("Default provider for infrastructure service {0} has been set to a custom action", serviceType.FullName);
        }


        /// <summary>
        /// Sets the default provider for the service to the give type. If the service is enabled the type will be registered
        /// in the container with the specified lifecycle
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="providerType"></param>
        /// <param name="dependencyLifecycle"></param>
        public static void SetDefaultFor<T>(Type providerType, DependencyLifecycle dependencyLifecycle)
        {
            var serviceType = typeof(T);

            SettingsHolder.SetDefault<T>(() => Configure.Component(providerType, dependencyLifecycle));
            SetStatusToDisabled(serviceType);
            Logger.DebugFormat("Default provider for infrastructure service {0} has been set to {1}, lifecycle: {2}", serviceType.FullName, providerType.FullName, dependencyLifecycle);
        }

        /// <summary>
        ///  Register a explicit service provider
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configAction"></param>
        public static void RegisterServiceFor<T>(Action configAction)
        {
            var serviceType = typeof(T);

            SettingsHolder.Set<T>(configAction);
            Logger.InfoFormat("Explicit provider for infrastructure service {0} has been set to custom action", serviceType.FullName);
        }

        /// <summary>
        /// Register a explicit service provider
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="providerType"></param>
        /// <param name="dependencyLifecycle"></param>
        public static void RegisterServiceFor<T>(Type providerType, DependencyLifecycle dependencyLifecycle)
        {
            var serviceType = typeof(T);

            SettingsHolder.Set<T>(() => Configure.Component(providerType, dependencyLifecycle));
            Logger.InfoFormat("Explicit provider for infrastructure service {0} has been set to {1}, lifecycle: {2}", serviceType.FullName, providerType.FullName, dependencyLifecycle);
        }


        /// <summary>
        /// Returns true if the requested service is available and can be enabled on demand
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool IsAvailable<T>()
        {
            return SettingsHolder.HasSetting<T>();
        }

        static void SetStatusToEnabled(Type serviceType)
        {
            knownServices[serviceType] = new ServiceStatus { Service = serviceType, Enabled = true };
        }

        static void SetStatusToDisabled(Type serviceType)
        {
            knownServices[serviceType] = new ServiceStatus { Service = serviceType, Enabled = false };
        }

        static readonly IDictionary<Type, ServiceStatus> knownServices = new Dictionary<Type, ServiceStatus>();
        static readonly ILog Logger = LogManager.GetLogger(typeof(InfrastructureServices));


        public class ServiceStatus
        {
            public Type Service { get; set; }
            public bool Enabled { get; set; }

        }

 
    }

    
    /// <summary>
    /// Displays the current status for the infrastructure services
    /// </summary>
    public class DisplayInfrastructureServicesStatus:IWantToRunWhenConfigurationIsComplete
    {
        public void Run()
        {
            var statusText = new StringBuilder();

            foreach (var serviceStatus in InfrastructureServices.CurrentStatus)
            {
                var provider = "Not defined";

                if (Configure.HasComponent(serviceStatus.Service))
                {
                    provider = Configure.Instance.Builder.Build(serviceStatus.Service).GetType().FullName;
                }

                statusText.AppendLine(string.Format("{0} provided by {1} - {2}", serviceStatus.Service.Name, provider, serviceStatus.Enabled ? "Enabled" : "Disabled"));
            }

            Logger.DebugFormat("Infrastructure services: \n{0}",statusText);
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(DisplayInfrastructureServicesStatus));
    }
}