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

            if (options.TimeToBeReceived.HasValue)
            {
                result["TimeToBeReceived"] = options.TimeToBeReceived.ToString();
            }

            if (options.NonDurable.HasValue && options.NonDurable.Value)
            {
                result["NonDurable"] = true.ToString();
            }

            if (options.ReplyToAddress != null)
            {
                result["ReplyToAddress"] = options.ReplyToAddress;
            }

            result["EnlistInReceiveTransaction"] = options.EnlistInReceiveTransaction.ToString();
            result["EnforceMessagingBestPractices"] = options.EnforceMessagingBestPractices.ToString();

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

            result["Operation"] = operation;


            return result;
        }

        public static DeliveryOptions ToDeliveryOptions(this Dictionary<string, string> options)
        {
            var operation = options["Operation"].ToLower();


            DeliveryOptions result;


            switch (operation)
            {
                case "publish":
                    result = new PublishOptions(Type.GetType(options["EventType"]))
                    {
                        ReplyToAddress = options["ReplyToAddress"]
                    };
                    break;
                case "send":
                case "audit":
                    var sendOptions = new SendOptions(options["Destination"]);

                    ApplySendOptionSettings(sendOptions, options);

                    result = sendOptions;
                    break;
                case "reply":
                    var replyOptions = new ReplyOptions(options["Destination"], options["CorrelationId"])
                    {
                        ReplyToAddress = options["ReplyToAddress"]
                    };
                    ApplySendOptionSettings(replyOptions, options);

                    result = replyOptions;
                    break;

                default:
                    throw new Exception("Unknown operation: " + operation);
            }


            string timeToBeReceived;
            if (options.TryGetValue("TimeToBeReceived", out timeToBeReceived))
            {
                result.TimeToBeReceived = TimeSpan.Parse(timeToBeReceived);
            }

            string nonDurable;
            if (options.TryGetValue("NonDurable", out nonDurable))
            {
                result.NonDurable = bool.Parse(nonDurable);
            }

            string enlistInReceiveTransaction;
            if (options.TryGetValue("EnlistInReceiveTransaction", out enlistInReceiveTransaction))
            {
                result.EnlistInReceiveTransaction = bool.Parse(enlistInReceiveTransaction);
            }

            string enforceMessagingBestPractices;
            if (options.TryGetValue("EnforceMessagingBestPractices", out enforceMessagingBestPractices))
            {
                result.EnforceMessagingBestPractices = bool.Parse(enforceMessagingBestPractices);
            }


            return result;
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