using System;
using NServiceBus.Timeout.Hosting.Windows.Persistence;
using Raven.Client;

namespace NServiceBus
{
    public static class ConfigureTimeoutExtensions
    {
        public static Action DefaultPersistence = () => Configure.Instance.UseRavenTimeoutPersister();

        [ObsoleteEx(Message = "As Timeout manager is a core functionality of NServiceBus it will be impossible to disable it beginning version 4.0.", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static Configure DisableTimeoutManager(this Configure config)
        {
            return config;
        }
        /// <summary>
        /// Use the in memory timeout persister implementation.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure UseInMemoryTimeoutPersister(this Configure config)
        {
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
            if (!config.Configurer.HasComponent<IDocumentStore>())
                config.RavenPersistence();

            config.Configurer.ConfigureComponent<RavenTimeoutPersistence>(DependencyLifecycle.SingleInstance);

            return config;
        }
    }
}