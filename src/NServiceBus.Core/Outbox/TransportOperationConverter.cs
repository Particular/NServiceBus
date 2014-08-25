namespace NServiceBus.Outbox
{
    using System;
    using System.Collections.Generic;
    using Unicast;

    static class TransportOperationConverter
    {
        public static Dictionary<string, string> ToTransportOperationOptions(this DeliveryOptions options, bool isAudit = false)
        {
            var result = new Dictionary<string, string>();

            var sendOptions = options as SendOptions;

            string operation;

            if (sendOptions != null)
            {
                operation = sendOptions is ReplyOptions ? "Reply" : "Send";

                if (isAudit)
                {
                    operation = "Audit";
                }

                if (sendOptions.DelayDeliveryWith.HasValue)
                {
                    result["DelayDeliveryWith"] = sendOptions.DelayDeliveryWith.Value.ToString();
                }

                if (sendOptions.DeliverAt.HasValue)
                {
                    result["DeliverAt"] = DateTimeExtensions.ToWireFormattedString(sendOptions.DeliverAt.Value);
                }


                if (sendOptions.TimeToBeReceived.HasValue)
                {
                    result["TimeToBeReceived"] = sendOptions.TimeToBeReceived.ToString();
                }

                result["CorrelationId"] = sendOptions.CorrelationId;
                result["Destination"] = sendOptions.Destination;
            }
            else
            {
                var publishOptions = options as PublishOptions;

                if (publishOptions == null)
                {
                    throw new Exception("Unknown delivery option: " + options.GetType().FullName);
                }

                operation = "Publish";
                result["EventType"] = publishOptions.EventType.AssemblyQualifiedName;
            }

            if (options.ReplyToAddress != null)
            {
                result["ReplyToAddress"] = options.ReplyToAddress;                
            }

            result["Operation"] = operation;


            return result;
        }

        public static DeliveryOptions ToDeliveryOptions(this Dictionary<string, string> options)
        {
            var operation = options["Operation"].ToLower();

            switch (operation)
            {
                case "publish":
                    return new PublishOptions(Type.GetType(options["EventType"]))
                    {
                        ReplyToAddress = options["ReplyToAddress"]
                    };

                case "send":
                case "audit":
                    var sendOptions = new SendOptions(options["Destination"]);

                    ApplySendOptionSettings(sendOptions, options);

                    return sendOptions;

                case "reply":
                    var replyOptions = new ReplyOptions(options["Destination"], options["CorrelationId"])
                    {
                        ReplyToAddress = options["ReplyToAddress"]
                    };
                    ApplySendOptionSettings(replyOptions, options);

                    return replyOptions;

                default:
                    throw new Exception("Unknown operation: " + operation);
            }


        }
        static void ApplySendOptionSettings(SendOptions sendOptions, Dictionary<string, string> options)
        {
            string delayDeliveryWith;
            if (options.TryGetValue("DelayDeliveryWith", out delayDeliveryWith))
            {
                sendOptions.DelayDeliveryWith = TimeSpan.Parse(delayDeliveryWith);
            }

            string deliverAt;
            if (options.TryGetValue("DeliverAt", out deliverAt))
            {
                sendOptions.DeliverAt = DateTimeExtensions.ToUtcDateTime(deliverAt);
            }

            string timeToBeReceived;
            if (options.TryGetValue("TimeToBeReceived", out timeToBeReceived))
            {
                sendOptions.TimeToBeReceived = TimeSpan.Parse(timeToBeReceived);
            }

            sendOptions.CorrelationId = options["CorrelationId"];
           
            string replyToAddress;
            if (options.TryGetValue("ReplyToAddress", out replyToAddress))
            {
                sendOptions.ReplyToAddress = replyToAddress;
            }


            sendOptions.CorrelationId = options["CorrelationId"];
        }

    }
}