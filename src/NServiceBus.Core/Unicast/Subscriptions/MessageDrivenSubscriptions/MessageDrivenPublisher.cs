namespace NServiceBus.Features
{
    using Config;
    using Logging;
    using Settings;
    using Transports;
    using Unicast.Publishing;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public class MessageDrivenPublisher : IFeature
    {
        public void Initialize()
        {
            var transportDefinition = SettingsHolder.GetOrDefault<TransportDefinition>("NServiceBus.Transport.SelectedTransport");

            if (transportDefinition != null && transportDefinition.HasNativePubSubSupport)
            {
                Logger.WarnFormat("The MessageDrivenPublisher feature is enabled but the transport has native pub/sub capabilities. Feature will not be initalized. This is most likely happening because you're specifying the As_a_Publisher role which is only relevant for transports without native pub/sub like Msmq, SqlServer etc");
                return;
            }

            Configure.Component<StorageDrivenPublisher>(DependencyLifecycle.InstancePerCall);

            InfrastructureServices.Enable<ISubscriptionStorage>();
        }

        static ILog Logger = LogManager.GetLogger(typeof(MessageDrivenPublisher));
    }


}