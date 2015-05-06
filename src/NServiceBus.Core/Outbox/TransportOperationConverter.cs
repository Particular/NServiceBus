namespace NServiceBus.Outbox
{
    using System;
    using System.Collections.Generic;
    using Unicast;

    static class TransportOperationConverter
    {
        public static Dictionary<string, string> ToTransportOperationOptions(this DeliveryMessageOptions options)
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

            return result;
        }

        public static DeliveryMessageOptions ToDeliveryOptions(this Dictionary<string, string> options)
        {
            DeliveryMessageOptions result;

            string destination;
            if (options.TryGetValue("Destination", out destination))
            {
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

                result = new SendMessageOptions(options["Destination"], deliverAt, delayDeliveryFor);
            }
            else
            {
                result = new DeliveryMessageOptions();
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

            return result;
        }
    }
}