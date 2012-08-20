using System;
using NServiceBus.Timeout.Hosting.Windows;
using NServiceBus.Timeout.Hosting.Windows.Persistence;
using Raven.Client;

namespace NServiceBus
{
    public static class ConfigureTimeoutExtensions
    {
        public static Action DefaultPersistence = () => Configure.Instance.UseRavenTimeoutPersister();

        public static Configure DisableTimeoutManager(this Configure config)
        {
            //make sure to disable it because satellite will try to bring it up
            if (config.Configurer.HasComponent<TimeoutMessageProcessor>())
                config.Configurer.ConfigureProperty<TimeoutMessageProcessor>(p => p.Disabled, true);
            else 
                config.Configurer.ConfigureComponent<TimeoutMessageProcessor>(DependencyLifecycle.SingleInstance)
                    .ConfigureProperty(p => p.Disabled, true);

            TimeoutManagerConfiguration.IsDisabled = true;

            return config;
        }
        /// <summary>
        /// Use the in memory timeout persister implementation.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure UseInMemoryTimeoutPersister(this Configure config)
        {
            if (TimeoutManagerConfiguration.IsDisabled)
                return config;
            config.Configurer.ConfigureComponent<InMemoryTimeoutPersistence>(DependencyLifecycle.SingleInstance);
            return config;
        }

        /// <summary>
        /// Sets the default persistence to UseInMemoryTimeoutPersister
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure DefaultToInMemoryTimeoutPersistence(this Configure config)
        {
            DefaultPersistence = () => UseInMemoryTimeoutPersister(config);
            return config;
        }

        /// <summary>
        /// Use the Raven timeout persister implementation.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure UseRavenTimeoutPersister(this Configure config)
        {
            if (TimeoutManagerConfiguration.IsDisabled)
                return config;
            if (!config.Configurer.HasComponent<IDocumentStore>())
                config.RavenPersistence();

            config.Configurer.ConfigureComponent<RavenTimeoutPersistence>(DependencyLifecycle.SingleInstance);

            return config;
        }
    }
}