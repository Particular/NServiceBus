namespace NServiceBus.Features
{
    using Logging;
    using Transports;

    /// <summary>
    /// Adds support for pub/sub using a external subscription storage. This brings pub/sub to transport that lacks native support.
    /// </summary>
    public class StorageDrivenPublisher : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            var transportDefinition = context.Settings.GetOrDefault<TransportDefinition>("NServiceBus.Transport.SelectedTransport");

            if (transportDefinition != null && transportDefinition.HasNativePubSubSupport)
            {
                Logger.WarnFormat("The StorageDrivenPublisher feature is enabled but the transport has native pub/sub capabilities. Feature will not be initialized. This is most likely happening because you're specifying the As_a_Publisher role which is only relevant for transports without native pub/sub like Msmq, SqlServer etc");
                return;
            }

            context.Container.ConfigureComponent<Unicast.Publishing.StorageDrivenPublisher>(DependencyLifecycle.InstancePerCall);
        }

        static ILog Logger = LogManager.GetLogger<StorageDrivenPublisher>();
    }
}