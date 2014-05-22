namespace NServiceBus.Outbox
{
    using System;
    using System.Collections.Generic;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;
    using Unicast.Behaviors;
    using Unicast.Messages;
    using NServiceBus;
    class OutboxDeduplicationBehavior : IBehavior<IncomingContext>
    {
        public IOutboxStorage OutboxStorage { get; set; }
        public DispatchMessageToTransportBehavior DispatchMessageToTransportBehavior { get; set; }

        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            var messageId = context.PhysicalMessage.Id;
            OutboxMessage outboxMessage;

            if (!OutboxStorage.TryGet(messageId, out outboxMessage))
            {
                outboxMessage = new OutboxMessage(messageId);

                context.Set(outboxMessage);

                next();
            }

            DispatchOperationToTransport(outboxMessage.TransportOperations);

            OutboxStorage.SetAsDispatched(messageId);
        }

        void DispatchOperationToTransport(IEnumerable<TransportOperation> operations)
        {
            foreach (var transportOperation in operations)
            {
                var deliveryOptions = transportOperation.Options.ToDeliveryOptions();

                //deliveryOptions.EnlistInReceiveTransaction = false;

                var message = new TransportMessage(transportOperation.MessageId, transportOperation.Headers)
                {
                    Body = transportOperation.Body
                };

                //dispatch to transport
                DispatchMessageToTransportBehavior.InvokeNative(deliveryOptions, message);
            }
        }

      
    }

    static class TransportOperationConverter
    {
        public static Dictionary<string, string> ToTransportOperationOptions(this DeliveryOptions options)
        {
            var result = new Dictionary<string, string>();

            var sendOptions = options as SendOptions;

            string operation;

            if (sendOptions != null)
            {
                operation = sendOptions is ReplyOptions ? "Reply" : "Send";

                if (sendOptions.DelayDeliveryWith.HasValue)
                {
                    result["DelayDeliveryWith"] = sendOptions.DelayDeliveryWith.Value.ToString();
                }

                if (sendOptions.DeliverAt.HasValue)
                {
                    result["DeliverAt"] = DateTimeExtensions.ToWireFormattedString(sendOptions.DeliverAt.Value);
                }

                result["CorrelationId"] = sendOptions.CorrelationId;
                result["Destination"] = sendOptions.Destination.ToString();
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

            result["ReplyToAddress"] = options.ReplyToAddress.ToString();
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
                        ReplyToAddress =  Address.Parse(options["ReplyToAddress"])
                    };

                case "send":
                    var sendOptions = new SendOptions(options["Destination"]);

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
                    sendOptions.ReplyToAddress = Address.Parse(options["ReplyToAddress"]);
                    return sendOptions;

                case "reply":
                    return new ReplyOptions(Address.Parse(options["Destination"]), options["CorrelationId"])
                    {
                        ReplyToAddress = Address.Parse(options["ReplyToAddress"])
                    };
                    
                default:
                    throw new Exception("Unknown operation: " + operation);
            }
        }


    }
}
