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

            result["EnlistInReceiveTransaction"] = options.EnlistInReceiveTransaction.ToString();
            result["EnforceMessagingBestPractices"] = options.EnforceMessagingBestPractices.ToString();

            var sendOptions = options as SendOptions;

            string operation;

            if (sendOptions != null)
            {
                operation = "Send";

                if (isAudit)
                {
                    operation = "Audit";
                }

                if (sendOptions.DelayDeliveryFor.HasValue)
                {
                    result["DelayDeliveryFor"] = sendOptions.DelayDeliveryFor.Value.ToString();
                }

                if (sendOptions.DeliverAt.HasValue)
                {
                    result["DeliverAt"] = DateTimeExtensions.ToWireFormattedString(sendOptions.DeliverAt.Value);
                }

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
                    result = new PublishOptions(Type.GetType(options["EventType"]));
                    break;
                case "send":
                case "audit":
                    string delayDeliveryForString;
                    TimeSpan? delayDeliveryFor = null;
                    if (options.TryGetValue("DelayDeliveryFor", out delayDeliveryForString))
                    {
                        delayDeliveryFor = TimeSpan.Parse(delayDeliveryForString);
                    }

                    string deliverAtString;
                    DateTime? deliverAt = null;
                    if (options.TryGetValue("DeliverAt", out deliverAtString))
                    {
                        deliverAt = DateTimeExtensions.ToUtcDateTime(deliverAtString);
                    }

                    result = new SendOptions(options["Destination"], deliverAt, delayDeliveryFor);

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
    }
}