namespace NServiceBus
{
    static class ActivityTags
    {
        public const string SagaId = "nservicebus.saga_id";
        public const string MessageId = "nservicebus.message_id";
        public const string CorrelationId = "nservicebus.correlation_id";
        public const string ReplyToAddress = "nservicebus.reply_to_address";
        public const string NServiceBusVersion = "nservicebus.version";
        public const string ControlMessageHeader = "nservicebus.control_message";
        public const string SagaType = "nservicebus.saga_type";
        public const string OriginatingSagaId = "nservicebus.originating_saga_id";
        public const string OriginatingSagaType = "nservicebus.originating_saga_type";
        public const string DelayedRetries = "nservicebus.delayed_retries";
        public const string DelayedRetriesTimestamp = "nservicebus.delayed_retries_timestamp";
        public const string DeliverAt = "nservicebus.deliver_at";
        public const string RelatedTo = "nservicebus.related_to";
        public const string EnclosedMessageTypes = "nservicebus.enclosed_message_types";
        public const string ContentType = "nservicebus.content_type";
        public const string IsSagaTimeoutMessage = "nservicebus.is_saga_timeout";
        public const string IsDeferredMessage = "nservicebus.is_deferred";
        public const string OriginatingEndpoint = "nservicebus.originating_endpoint";
        public const string OriginatingMachine = "nservicebus.originating_machine";
        public const string OriginatingHostId = "nservicebus.originating_host_id";
        public const string ProcessingEndpoint = "nservicebus.processing_endpoint";
        public const string ProcessingMachine = "nservicebus.processing_machine";
        public const string HostDisplayName = "nservicebus.host_display_name";
        public const string HostId = "nservicebus.host_id";
        public const string HasLicenseExpired = "nservicebus.has_license_expired";
        public const string OriginatingAddress = "nservicebus.originating_address";
        public const string ConversationId = "nservicebus.conversation_id";
        public const string PreviousConversationId = "nservicebus.previous_conversation_id";
        public const string MessageIntent = "nservicebus.message_intent";
        public const string TimeToBeReceived = "nservicebus.time_to_be_received";
        public const string DataBusContentType = "nservicebus.databus.content_type";
        public const string NonDurableMessage = "nservicebus.non_durable";
        public const string NativeMessageId = "nservicebus.native_message_id";
        public const string HandlerType = "nservicebus.handler.handler_type";
        public const string HandlerSagaId = "nservicebus.handler.saga_id";
        public const string EventTypes = "nservicebus.event_types";
        public const string CancelledTask = "nservicebus.cancelled";
    }
}