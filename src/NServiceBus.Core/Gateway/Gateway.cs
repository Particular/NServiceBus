namespace NServiceBus.Features
{
    using System;
    using System.Linq;
    using Config;
    using NServiceBus.Gateway.Channels;
    using NServiceBus.Gateway.Deduplication;
    using NServiceBus.Gateway.HeaderManagement;
    using NServiceBus.Gateway.Notifications;
    using NServiceBus.Gateway.Persistence;
    using NServiceBus.Gateway.Receiving;
    using NServiceBus.Gateway.Routing.Endpoints;
    using NServiceBus.Gateway.Routing.Sites;
    using NServiceBus.Gateway.Sending;

    public class Gateway : Feature
    {
        public override void Initialize(Configure config)
        {
            ConfigureChannels(config);

            ConfigureReceiver(config);

            ConfigureSender(config);

            InfrastructureServices.Enable<IPersistMessages>();
            InfrastructureServices.Enable<IDeduplicateMessages>();
        }

        static void ConfigureChannels(Configure config)
        {
            var channelFactory = new ChannelFactory();

            foreach (
                var type in
                    Configure.TypesToScan.Where(t => typeof(IChannelReceiver).IsAssignableFrom(t) && !t.IsInterface))
            {
                channelFactory.RegisterReceiver(type);
            }

            foreach (
                var type in
                    Configure.TypesToScan.Where(t => typeof(IChannelSender).IsAssignableFrom(t) && !t.IsInterface))
            {
                channelFactory.RegisterSender(type);
            }

            config.Configurer.RegisterSingleton<IChannelFactory>(channelFactory);
        }

        static void ConfigureSender(Configure config)
        {
            if (!config.Configurer.HasComponent<IForwardMessagesToSites>())
            {
                config.Configurer.ConfigureComponent<IdempotentChannelForwarder>(DependencyLifecycle.InstancePerCall);
                config.Configurer.ConfigureComponent<SingleCallChannelForwarder>(DependencyLifecycle.InstancePerCall);
            }

            config.Configurer.ConfigureComponent<MessageNotifier>(DependencyLifecycle.SingleInstance);

            var configSection = Configure.GetConfigSection<GatewayConfig>();

            if (configSection != null && configSection.GetChannels().Any())
            {
                config.Configurer.ConfigureComponent<ConfigurationBasedChannelManager>(DependencyLifecycle.SingleInstance);
            }
            else
            {
                config.Configurer.ConfigureComponent<ConventionBasedChannelManager>(DependencyLifecycle.SingleInstance);
            }

            ConfigureSiteRouters(config);
        }

        static void ConfigureSiteRouters(Configure config)
        {
            config.Configurer.ConfigureComponent<OriginatingSiteHeaderRouter>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<KeyPrefixConventionSiteRouter>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<ConfigurationBasedSiteRouter>(DependencyLifecycle.SingleInstance);
        }

        static void ConfigureReceiver(Configure config)
        {
            if (!config.Configurer.HasComponent<IReceiveMessagesFromSites>())
            {
                config.Configurer.ConfigureComponent<IdempotentChannelReceiver>(DependencyLifecycle.InstancePerCall);
                config.Configurer.ConfigureComponent<SingleCallChannelReceiver>(DependencyLifecycle.InstancePerCall);
                config.Configurer.ConfigureComponent<Func<IReceiveMessagesFromSites>>(builder => () => builder.Build<SingleCallChannelReceiver>(), DependencyLifecycle.InstancePerCall);
            }
            else
            {
                config.Configurer.ConfigureComponent<Func<IReceiveMessagesFromSites>>(builder => () => builder.Build<IReceiveMessagesFromSites>(), DependencyLifecycle.InstancePerCall);
            }

            config.Configurer.ConfigureComponent<DataBusHeaderManager>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<DefaultEndpointRouter>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.MainInputAddress, Address.Parse(Configure.EndpointName));
        }
    }
}