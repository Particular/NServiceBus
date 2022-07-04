namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Diagnostics;

    class ActivityDecorator
    {
        public static void PromoteHeadersToTags(Activity activity, Dictionary<string, string> headers)
        {
            if (activity == null)
            {
                return;
            }

            foreach (var header in headers)
            {
                if (HeaderMapping.TryGetValue(header.Key, out var tagName))
                {
                    activity.AddTag(tagName, header.Value);
                }
            }
        }

        internal static readonly Dictionary<string, string> HeaderMapping = new()
        {
            //{ Headers.HttpFrom, "" },
            //{ Headers.HttpTo, "" },
            //{ Headers.RouteTo, "" },
            //{ Headers.DestinationSites, "" },
            //{ Headers.OriginatingSite, "" },
            { Headers.SagaId, "nservicebus.saga_id" },
            { Headers.MessageId, "nservicebus.message_id" },
            { Headers.CorrelationId, "nservicebus.correlation_id" },
            { Headers.ReplyToAddress, "nservicebus.reply_to_address" },
            { Headers.NServiceBusVersion, "nservicebus.version" },
            //{ Headers.ReturnMessageErrorCodeHeader, "" },
            { Headers.ControlMessageHeader, "nservicebus.control_message" },
            { Headers.SagaType, "nservicebus.saga_type" },
            { Headers.OriginatingSagaId, "nservicebus.originating_saga_id" },
            { Headers.OriginatingSagaType, "nservicebus.originating_saga_type" },
            { Headers.DelayedRetries, "nservicebus.delayed_retries" },
            { Headers.DelayedRetriesTimestamp, "nservicebus.delayed_retries_timestamp" },
            //{ Headers.ImmediateRetries, "" },
            //{ Headers.ProcessingStarted, "" },
            //{ Headers.ProcessingEnded, "" },
            //{ Headers.TimeSent, "" },
            { Headers.DeliverAt, "nservicebus.deliver_at" },
            { Headers.RelatedTo, "nservicebus.related_to" },
            { Headers.EnclosedMessageTypes, "nservicebus.enclosed_message_types" },
            { Headers.ContentType, "nservicebus.content_type" },
            //{ Headers.SubscriptionMessageType, "nservicebus.subscription_message_type" }, // subscription headers are not promoted to tags
            //{ Headers.SubscriberTransportAddress, "nservicebus.subscriber_address" },
            //{ Headers.SubscriberEndpoint, "nservicebus.subscriber_endpoint" },
            { Headers.IsSagaTimeoutMessage, "nservicebus.is_saga_timeout" },
            { Headers.IsDeferredMessage, "nservicebus.is_deferred" },
            { Headers.OriginatingEndpoint, "nservicebus.originating_endpoint" },
            { Headers.OriginatingMachine, "nservicebus.originating_machine" },
            { Headers.OriginatingHostId, "nservicebus.originating_host_id" },
            { Headers.ProcessingEndpoint, "nservicebus.processing_endpoint" },
            { Headers.ProcessingMachine, "nservicebus.processing_machine" },
            { Headers.HostDisplayName, "nservicebus.host_display_name" },
            { Headers.HostId, "nservicebus.host_id" },
            { Headers.HasLicenseExpired, "nservicebus.has_license_expired" },
            { Headers.OriginatingAddress, "nservicebus.originating_address" },
            { Headers.ConversationId, "nservicebus.conversation_id" },
            { Headers.PreviousConversationId, "nservicebus.previous_conversation_id" },
            { Headers.MessageIntent, "nservicebus.message_intent" },
            { Headers.TimeToBeReceived, "nservicebus.time_to_be_received" },
            //{ Headers.DiagnosticsTraceParent, "" },
            //{ Headers.DiagnosticsTraceState, "" },
            //{ Headers.DiagnosticsBaggage, "" },
            { Headers.DataBusContentType, "nservicebus.databus.content_type" },
            //{ Headers.HeaderName, "" },
            { Headers.NonDurableMessage, "nservicebus.non_durable" }
        };
    }
}