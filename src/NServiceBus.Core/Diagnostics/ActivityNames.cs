namespace NServiceBus
{
    class ActivityNames
    {
        // TODO: do we want to stick with incoming and outgoing message or use the term send instead?
        public const string IncomingMessageActivityName = "NServiceBus.Diagnostics.IncomingMessage";
        public const string OutgoingMessageActivityName = "NServiceBus.Diagnostics.OutgoingMessage";
        public const string OutgoingEventActivityName = "NServiceBus.Diagnostics.PublishMessage";
        public const string SubscribeActivityName = "NServiceBus.Diagnostics.Subscribe";
    }

    class DiagnosticsKeys
    {
        public const string OutgoingActivityKey = "NServiceBus.Diagnostics.Activity.Outgoing";
    }
}