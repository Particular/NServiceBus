namespace NServiceBus.Diagnostics
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using Pipeline;
    using Transport;

    class ActivityDecorator
    {
        public static void SetReplyTags(Activity activity, string replyToAddress, IOutgoingReplyContext context)
        {
            if (activity != null)
            {
                activity.SetTag("NServiceBus.MessageId", context.MessageId);
                activity.SetTag("NServiceBus.ReplyToAddress", replyToAddress);
            }
        }

        public static void InjectHeaders(Activity activity, Dictionary<string, string> headers)
        {
            if (activity != null)
            {
                headers.Add("traceparent", activity.Id);
                if (activity.TraceStateString != null)
                {
                    headers["tracestate"] = activity.TraceStateString;
                }
            }
        }

        public static void SetSendTags(Activity activity, IOutgoingSendContext context)
        {
            activity?.SetTag("NServiceBus.MessageId", context.MessageId);
        }

        public static void SetReceiveTags(Activity activity, string endpointQueueName, IncomingMessage message)
        {
            if (activity == null)
            {
                return;
            }

            activity.AddTag("NServiceBus.MessageId", message.MessageId);
            var operation = "process";

            // TODO: Set destination properly
            activity.DisplayName = $"{endpointQueueName} {operation}";
            activity.AddTag("messaging.operation", operation);
            activity.AddTag("messaging.destination", endpointQueueName);
            activity.AddTag("messaging.message_id", message.MessageId);
            activity.AddTag("messaging.message_payload_size_bytes", message.Body.Length.ToString());

            if (message.Headers.TryGetValue(Headers.ConversationId, out var conversationId))
            {
                activity.AddTag("messaging.conversation_id", conversationId);
            }
        }
    }
}