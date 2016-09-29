namespace NServiceBus.Features
{
    using System.Linq;
    using DeliveryConstraints;
    using Performance.TimeToBeReceived;
    using Unicast.Messages;

    class TimeToBeReceived : Feature
    {
        public TimeToBeReceived()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var mappings = GetMappings(context);

            context.Pipeline.Register("ApplyTimeToBeReceived", new ApplyTimeToBeReceivedBehavior(mappings), "Adds the `DiscardIfNotReceivedBefore` constraint to relevant messages");
        }

        static TimeToBeReceivedMappings GetMappings(FeatureConfigurationContext context)
        {
            var registry = context.Settings.Get<MessageMetadataRegistry>();
            var knownMessages = registry.GetAllMessages().Select(m => m.MessageType);

            var convention = TimeToBeReceivedMappings.DefaultConvention;

            UserDefinedTimeToBeReceivedConvention userDefinedConvention;
            if (context.Settings.TryGet(out userDefinedConvention))
            {
                convention = userDefinedConvention.GetTimeToBeReceivedForMessage;
            }

            var doesTransportSupportDiscardIfNotReceivedBefore = context.Settings.DoesTransportSupportConstraint<DiscardIfNotReceivedBefore>();
            return new TimeToBeReceivedMappings(knownMessages, convention, doesTransportSupportDiscardIfNotReceivedBefore);
        }
    }
}