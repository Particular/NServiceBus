namespace NServiceBus.Features
{
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
        public override void Initialize()
        {
            ConfigureChannels();

            ConfigureReceiver();

            ConfigureSender();

            InfrastructureServices.Enable<IPersistMessages>();
            InfrastructureServices.Enable<IDeduplicateMessages>();
        }

        static void ConfigureChannels()
        {
            var registry = new ChannelTypeRegistry();

            FillChannelTypeRegistry(registry);

            Configure.Instance.Configurer.RegisterSingleton<ChannelTypeRegistry>(registry);
            Configure.Instance.Configurer.ConfigureComponent<ChannelFactory>(DependencyLifecycle.SingleInstance);
        }

        static void FillChannelTypeRegistry(IChannelTypeRegistry registry)
        {
            foreach (var type in Configure.TypesToScan.Where(t => typeof(IChannelReceiver).IsAssignableFrom(t) && !t.IsInterface))
            {
                var channelTypes = type.GetCustomAttributes(true).OfType<ChannelTypeAttribute>().ToList();
                if (channelTypes.Any())
                {
                    channelTypes.ForEach(t => registry.AddReceiver(t.Type, type));
                }
                else
                {
                    registry.AddReceiver(type.Name.Substring(0, type.Name.IndexOf("Channel")), type);
                }
            }

            foreach (var type in Configure.TypesToScan.Where(t => typeof(IChannelSender).IsAssignableFrom(t) && !t.IsInterface))
            {
                var channelTypes = type.GetCustomAttributes(true).OfType<ChannelTypeAttribute>().ToList();
                if (channelTypes.Any())
                {
                    channelTypes.ForEach(t => registry.AddSender(t.Type, type));
                }
                else
                {
                    registry.AddSender(type.Name.Substring(0, type.Name.IndexOf("Channel")), type);
                }
            }
        }

        static void ConfigureSender()
        {
            if (!Configure.Instance.Configurer.HasComponent<IForwardMessagesToSites>())
            {
                Configure.Component<IdempotentChannelForwarder>(DependencyLifecycle.InstancePerCall);
                Configure.Component<SingleCallChannelForwarder>(DependencyLifecycle.InstancePerCall);
            }

            Configure.Component<MessageNotifier>(DependencyLifecycle.SingleInstance);

            var configSection = Configure.GetConfigSection<GatewayConfig>();

            if (configSection != null && configSection.GetChannels().Any())
            {
                Configure.Component<ConfigurationBasedChannelManager>(DependencyLifecycle.SingleInstance);
            }
            else
            {
                Configure.Component<ConventionBasedChannelManager>(DependencyLifecycle.SingleInstance);
            }

            ConfigureSiteRouters();
        }

        static void ConfigureSiteRouters()
        {
            Configure.Component<OriginatingSiteHeaderRouter>(DependencyLifecycle.SingleInstance);
            Configure.Component<KeyPrefixConventionSiteRouter>(DependencyLifecycle.SingleInstance);
            Configure.Component<ConfigurationBasedSiteRouter>(DependencyLifecycle.SingleInstance);
        }

        static void ConfigureReceiver()
        {
            if (!Configure.Instance.Configurer.HasComponent<IReceiveMessagesFromSites>())
            {
                Configure.Component<IdempotentChannelReceiver>(DependencyLifecycle.InstancePerCall);
                Configure.Component<SingleCallChannelReceiver>(DependencyLifecycle.InstancePerCall);
            }

            Configure.Component<DataBusHeaderManager>(DependencyLifecycle.InstancePerCall);

            Configure.Component<DefaultEndpointRouter>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.MainInputAddress, Address.Parse(Configure.EndpointName));
        }
    }
}