namespace NServiceBus
{
    class ActivityNames
    {
        // TODO: do we want to stick with incoming and outgoing message or use the term send instead?
        public const string IncomingMessageActivityName = "NServiceBus.Diagnostics.IncomingMessage";
        public const string OutgoingMessageActivityName = "NServiceBus.Diagnostics.SendMessage";
        public const string OutgoingEventActivityName = "NServiceBus.Diagnostics.PublishMessage";
        public const string SubscribeActivityName = "NServiceBus.Diagnostics.Subscribe";
        public const string UnsubscribeActivityName = "NServiceBus.Diagnostics.Unsubscribe";
        public const string InvokeHandlerActivityName = "NServiceBus.Diagnostics.InvokeHandler";
    }
}