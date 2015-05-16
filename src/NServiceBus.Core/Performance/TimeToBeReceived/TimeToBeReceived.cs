namespace NServiceBus.Features
{
    using System;
    using System.Linq;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Performance.TimeToBeReceived;

    class TimeToBeReceived:Feature
    {
        public TimeToBeReceived()
        {
            EnableByDefault();
        }
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var mappings = GetMappings(context);

            if (mappings.HasEntries && !context.DoesTransportSupportConstraint<DiscardIfNotReceivedBefore>())
            {
                throw new Exception("Messages with TimeToBeReceived found but the selected transport does not support this type of restriction. Please remove TTBR from your messages, disable this feature or select a transport that does support TTBR");
            }

            context.Pipeline.Register("ApplyTimeToBeReceived", typeof(ApplyTimeToBeReceivedBehavior), "Adds the `DiscardIfNotReceivedBefore` constraint to relevant messages");

            context.Container.ConfigureComponent(b=>new ApplyTimeToBeReceivedBehavior(mappings), DependencyLifecycle.SingleInstance);
        }

        TimeToBeReceivedMappings GetMappings(FeatureConfigurationContext context)
        {   
            var knownMessages = context.Settings.GetAvailableTypes()
                .Where(context.Settings.Get<Conventions>().IsMessageType)
                .ToList();

            var convention = TimeToBeReceivedMappings.DefaultConvention;

            UserDefinedTimeToBeReceivedConvention userDefinedConvention;
            if (context.Settings.TryGet(out userDefinedConvention))
            {
                convention = userDefinedConvention.GetTimeToBeReceivedForMessage;
            }

            return new TimeToBeReceivedMappings(knownMessages,convention);
        }
    }
}