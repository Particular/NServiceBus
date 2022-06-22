namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Routing;
    using Transport;

    class ActivityDecorator
    {
        static string endpointQueueName;


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

        public static void SetOutgoingTraceTags(Activity activity, OutgoingMessage message, TransportOperation[] operations)
        {
            if (activity == null)
            {
                return;
            }

            activity.AddTag("NServiceBus.MessageId", message.MessageId);
            activity.AddTag("messaging.message_id", message.MessageId);

            if (message.Headers.TryGetValue(Headers.ConversationId, out var conversationId))
            {
                activity.AddTag("messaging.conversation_id", conversationId);
            }

            // HINT: This needs to be converted into a string or the tag is not created
            activity.AddTag("messaging.message_payload_size_bytes", message.Body.Length.ToString());
            activity.AddTag("messaging.operation", "send");

            var destinations = new string[operations.Length];
            var currentOperation = 0;
            var allUnicast = true;
            var allMulticast = true;
            foreach (var operation in operations)
            {
                if (operation.AddressTag is MulticastAddressTag m)
                {
                    destinations[currentOperation] = m.MessageType.FullName;
                    allUnicast = false;
                }
                else if (operation.AddressTag is UnicastAddressTag u)
                {
                    destinations[currentOperation] = u.Destination;
                    allMulticast = false;
                }

                currentOperation++;
            }

            if (allUnicast)
            {
                activity.AddTag("messaging.destination_kind", "queue");
            }

            if (allMulticast)
            {
                activity.AddTag("messaging.destination_kind", "topic");
            }

            var destination = string.Join(", ", destinations);
            activity.AddTag("messaging.destination", destination);
            activity.DisplayName = $"{destination} send";
        }

        public static void Initialize(string receiveAddress)
        {
            endpointQueueName = receiveAddress;
        }

        public static void PromoteHeadersToTags(Activity activity, Dictionary<string, string> headers)
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

        public static void SetErrorStatus(Activity activity, Exception ex)
        {
            if (activity == null)
            {
                return;
            }

            activity.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("otel.status_code", "ERROR");
            activity?.SetTag("otel.status_description", ex.Message);
        }

        // List of message headers that shouldn't be added as activity tags
        static readonly HashSet<string> IgnoreHeaders = new HashSet<string> { Headers.TimeSent };
    }
}