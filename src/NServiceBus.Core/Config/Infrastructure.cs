namespace NServiceBus.Config
{
    using System;
    using NServiceBus.Logging;
    using Settings;

    /// <summary>
    /// Class used to control the various infrastructure services required by NServiceBus
    /// </summary>
    public class Infrastructure
    {
        /// <summary>
        /// Enables the given infrastructure service by registering it in the container
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void Enable<T>()
        {
            var serviceType = typeof (T);

            if (Configure.Instance.Configurer.HasComponent<T>())
            {
                Logger.InfoFormat("Infrastructure service {0} was found in the container and will be used instead of the default",serviceType.FullName);

                //todo: We should guide users to register their infrastructure overrides in a more explicit way in the future
                return;
            }

            var configAction = SettingsHolder.Get<Action>(serviceType.FullName);

            configAction();

            Logger.DebugFormat("Infrastructure service {0} has been configured", serviceType.FullName);
        }

        /// <summary>
        /// Set the default for the infastructure service to the action passed in.
        /// If the service is enabled and no explict override is found this action will be used to configure the service.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configAction"></param>
        public static void SetDefaultFor<T>(Action configAction)
        {
            var serviceType = typeof(T);

            SettingsHolder.SetDefault<T>(configAction);

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

            Logger.DebugFormat("Default provider for infrastructure service {0} has been set to {1}, lifecycle: {2}",serviceType.FullName,providerType.FullName,dependencyLifecycle);
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(Infrastructure));
    }
}