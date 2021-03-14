namespace NServiceBus.Features
{
    using Transport;
    using System.Linq;
    using Unicast.Messages;
    using System.Threading.Tasks;
    using System.Threading;

    class TimeToBeReceived : Feature
    {
        public TimeToBeReceived()
        {
            EnableByDefault();
        }

        protected internal override Task Setup(FeatureConfigurationContext context, CancellationToken cancellationToken = default)
        {
            var mappings = GetMappings(context);

            context.Pipeline.Register("ApplyTimeToBeReceived", new ApplyTimeToBeReceivedBehavior(mappings), "Adds the `DiscardIfNotReceivedBefore` constraint to relevant messages");

            return Task.CompletedTask;
        }

        static TimeToBeReceivedMappings GetMappings(FeatureConfigurationContext context)
        {
            var registry = context.Settings.Get<MessageMetadataRegistry>();
            var knownMessages = registry.GetAllMessages().Select(m => m.MessageType);

            var convention = TimeToBeReceivedMappings.DefaultConvention;

            if (context.Settings.TryGet(out UserDefinedTimeToBeReceivedConvention userDefinedConvention))
            {
                convention = userDefinedConvention.GetTimeToBeReceivedForMessage;
            }

            var doesTransportSupportDiscardIfNotReceivedBefore = context.Settings.Get<TransportDefinition>().SupportsTTBR;
            return new TimeToBeReceivedMappings(knownMessages, convention, doesTransportSupportDiscardIfNotReceivedBefore);
        }
    }
}