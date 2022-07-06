namespace NServiceBus
{
    class ActivityNames
    {
        public const string IncomingMessageActivityName = "NServiceBus.Diagnostics.IncomingMessage";
        public const string OutgoingMessageActivityName = "NServiceBus.Diagnostics.OutgoingMessage";
        public const string OutgoingEventActivityName = "NServiceBus.Diagnostics.PublishMessage";
        public const string SubscribeActivityName = "NServiceBus.Diagnostics.Subscribe";
        public const string UnsubscribeActivityName = "NServiceBus.Diagnostics.Unsubscribe";
        public const string InvokeHandlerActivityName = "NServiceBus.Diagnostics.InvokeHandler";
    }
}