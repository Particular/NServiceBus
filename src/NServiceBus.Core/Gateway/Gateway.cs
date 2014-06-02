namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Config;
    using Installation;
    using NServiceBus.Gateway;
    using NServiceBus.Gateway.Channels;
    using NServiceBus.Gateway.HeaderManagement;
    using NServiceBus.Gateway.Notifications;
    using NServiceBus.Gateway.Receiving;
    using NServiceBus.Gateway.Routing;
    using NServiceBus.Gateway.Routing.Endpoints;
    using NServiceBus.Gateway.Routing.Sites;
    using NServiceBus.Gateway.Sending;

    /// <summary>
    /// Gateway
    /// </summary>
    public class Gateway : Feature
    {
        /// <summary>
        ///     Called when the features is activated
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {

            var txConfig = context.Container.ConfigureComponent<GatewayTransaction>(DependencyLifecycle.InstancePerCall);

            var configSection = context.Settings.GetConfigSection<GatewayConfig>();

            if (configSection != null)
            {
                txConfig.ConfigureProperty(c => c.ConfiguredTimeout, configSection.TransactionTimeout);
            }


            ConfigureChannels(context);

            ConfigureReceiver(context);

            ConfigureSender(context);
        }

        static void ConfigureChannels(FeatureConfigurationContext context)
        {
            var channelFactory = new ChannelFactory();

            foreach (
                var type in
                    context.Settings.GetAvailableTypes().Where(t => typeof(IChannelReceiver).IsAssignableFrom(t) && !t.IsInterface))
            {
                channelFactory.RegisterReceiver(type);
            }

            foreach (
                var type in
                    context.Settings.GetAvailableTypes().Where(t => typeof(IChannelSender).IsAssignableFrom(t) && !t.IsInterface))
            {
                channelFactory.RegisterSender(type);
            }

            context.Container.RegisterSingleton<IChannelFactory>(channelFactory);
        }

        static void ConfigureSender(FeatureConfigurationContext context)
        {
            if (!context.Container.HasComponent<IForwardMessagesToSites>())
            {
                context.Container.ConfigureComponent<SingleCallChannelForwarder>(DependencyLifecycle.InstancePerCall);
            }

            context.Container.ConfigureComponent<MessageNotifier>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<GatewaySender>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(t => t.Disabled, false);

            var configSection = context.Settings.GetConfigSection<GatewayConfig>();

            if (configSection != null && configSection.GetChannels().Any())
            {
                context.Container.ConfigureComponent<ConfigurationBasedChannelManager>(DependencyLifecycle.SingleInstance)
                    .ConfigureProperty(c => c.ReceiveChannels, configSection.GetChannels());
            }
            else
            {
                context.Container.ConfigureComponent<ConventionBasedChannelManager>(DependencyLifecycle.SingleInstance)
                    .ConfigureProperty(t => t.EndpointName, context.Settings.EndpointName());
            }

            ConfigureSiteRouters(context);
        }

        static void ConfigureSiteRouters(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<OriginatingSiteHeaderRouter>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<KeyPrefixConventionSiteRouter>(DependencyLifecycle.SingleInstance);

            IDictionary<string, Site> sites = new Dictionary<string, Site>();

            var section = context.Settings.GetConfigSection<GatewayConfig>();
            if (section != null)
            {
                sites = section.SitesAsDictionary();
            }

            context.Container.ConfigureComponent<ConfigurationBasedSiteRouter>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.Sites, sites);
        }



        static void ConfigureReceiver(FeatureConfigurationContext context)
        {
            if (!context.Container.HasComponent<IReceiveMessagesFromSites>())
            {
                context.Container.ConfigureComponent<SingleCallChannelReceiver>(DependencyLifecycle.InstancePerCall);
                context.Container.ConfigureComponent<Func<IReceiveMessagesFromSites>>(builder => () => builder.Build<SingleCallChannelReceiver>(), DependencyLifecycle.InstancePerCall);
            }
            else
            {
                context.Container.ConfigureComponent<Func<IReceiveMessagesFromSites>>(builder => () => builder.Build<IReceiveMessagesFromSites>(), DependencyLifecycle.InstancePerCall);
            }

            context.Container.ConfigureComponent<DataBusHeaderManager>(DependencyLifecycle.InstancePerCall);

            var endpointName = context.Settings.Get<string>("EndpointName");

            context.Container.ConfigureComponent<GatewayHttpListenerInstaller>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.Enabled, true);

            context.Container.ConfigureComponent<DefaultEndpointRouter>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.MainInputAddress, Address.Parse(endpointName));

            context.Container.ConfigureComponent<GatewayReceiver>(DependencyLifecycle.SingleInstance)
         .ConfigureProperty(t => t.Disabled, false);
        }
    }
}