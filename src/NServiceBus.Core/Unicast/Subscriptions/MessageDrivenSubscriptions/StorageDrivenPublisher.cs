namespace NServiceBus.Features
{
    using Config;
    using Logging;
    using Transports;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    /// <summary>
    /// Adds support for pub/sub using a external subscription storage. This brings pub/sub to transport that lacks native support.
    /// </summary>
    public class StorageDrivenPublisher : Feature
    {
        public override void Initialize(Configure config)
        {
            var transportDefinition = config.Settings.GetOrDefault<TransportDefinition>("NServiceBus.Transport.SelectedTransport");

            if (transportDefinition != null && transportDefinition.HasNativePubSubSupport)
            {
                Logger.WarnFormat("The StorageDrivenPublisher feature is enabled but the transport has native pub/sub capabilities. Feature will not be initialized. This is most likely happening because you're specifying the As_a_Publisher role which is only relevant for transports without native pub/sub like Msmq, SqlServer etc");
                return;
            }

            config.Configurer.ConfigureComponent<Unicast.Publishing.StorageDrivenPublisher>(DependencyLifecycle.InstancePerCall);

            InfrastructureServices.Enable<ISubscriptionStorage>();
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(StorageDrivenPublisher));
    }
}