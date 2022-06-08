namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Routing;
    using Transport;

    class ActivityDecorator
    {
        static string endpointQueueName;

        //TODO should this be moved somewhere else, naming indicates that we're adding headers to the activity
        public static void InjectHeaders(Activity activity, Dictionary<string, string> headers)
        {
            if (activity != null)
            {
                headers.Add(Headers.DiagnosticsTraceParent, activity.Id);
                if (activity.TraceStateString != null)
                {
                    headers[Headers.DiagnosticsTraceState] = activity.TraceStateString;
                }
            }
        }

        public static void SetReceiveTags(Activity activity, IncomingMessage message)
        {
            if (activity == null)
            {
                return;
            }

            var operation = "process";

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

        public static void SetOutgoingTraceTags(Activity activity, TransportOperation[] operations)
        {
            if (activity == null)
            {
                return;
            }

            var destinations = new string[operations.Length];
            var currentOperation = 0;
            // TODO: How do we handle multiple operations here?
            foreach (var operation in operations)
            {
                destinations[currentOperation] = operation.AddressTag switch
                {
                    UnicastAddressTag u => u.Destination,
                    MulticastAddressTag m => m.MessageType.FullName,
                    _ => null
                };

                activity.AddTag("NServiceBus.MessageId", operation.Message.MessageId);
                activity.AddTag("messaging.message_id", operation.Message.MessageId);

                if (operation.AddressTag is UnicastAddressTag unicastAddressTag)
                {
                    activity.AddTag("messaging.destination_kind", "queue");
                }
                else if (operation.AddressTag is MulticastAddressTag multicastAddressTag)
                {
                    activity.AddTag("messaging.destination_kind", "topic");
                }
                if (operation.Message.Headers.TryGetValue(Headers.ConversationId, out var conversationId))
                {
                    activity.AddTag("messaging.conversation_id", conversationId);
                }

                // HINT: This needs to be converted into a string or the tag is not created
                activity.AddTag("messaging.message_payload_size_bytes", operation.Message.Body.Length.ToString());

                currentOperation++;
            }

            activity.AddTag("messaging.operation", "send");
            var destination = string.Join(", ", destinations);
            activity.AddTag("messaging.destination", destination);
            activity.DisplayName = $"{destination} send";
        }

        public static void Initialize(string receiveAddress)
        {
            endpointQueueName = receiveAddress;
        }

        public static void SetHeaderTraceTags(Activity activity, Dictionary<string, string> headers)
        {
            if (activity == null)
            {
                return;
            }

            foreach (var header in headers)
            {
                if (header.Key.StartsWith("NServiceBus.") && !IgnoreHeaders.Contains(header.Key))
                {
                    activity.AddTag(OtNamingConvention(header.Key), header.Value);
                }
            }

            //TODO might be faster to just provide a hardcoded lookup if we're going with an allow-list approach
            string OtNamingConvention(string pascalCasedString)
            {
                // use "nservicebus" instead of "n_service_bus"
                pascalCasedString = pascalCasedString.Replace("NServiceBus", "nservicebus");
                var additionalLength =
                    pascalCasedString.Count(char.IsUpper) - pascalCasedString.Count(c => c == '.');
                var result = new char[pascalCasedString.Length + additionalLength];
                int i = 0;
                foreach (char c in pascalCasedString)
                {
                    if (char.IsUpper(c) && i > 0 && result[i - 1] != '.')
                    {
                        result[i] = '_';
                        i++;
                    }

                    result[i] = char.ToLower(c);
                    i++;
                }

                return new string(result);
            }
        }

        // List of message headers that shouldn't be added as activity tags
        static readonly HashSet<string> IgnoreHeaders = new HashSet<string> { Headers.TimeSent };
    }
}