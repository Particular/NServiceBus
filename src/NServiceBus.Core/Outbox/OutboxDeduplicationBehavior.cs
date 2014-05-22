namespace NServiceBus.Outbox
{
    using System;
    using System.Collections.Generic;
    using Pipeline;
    using Pipeline.Contexts;
    using Transports;
    using Unicast;
    using Unicast.Behaviors;
    using Unicast.Messages;
    using NServiceBus;
    class OutboxDeduplicationBehavior : IBehavior<IncomingContext>
    {
        public IOutboxStorage OutboxStorage { get; set; }
        public DispatchMessageToTransportBehavior DispatchMessageToTransportBehavior { get; set; }

        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }

        public DefaultMessageAuditer DefaultMessageAuditer { get; set; }

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

                deliveryOptions.EnlistInReceiveTransaction = false;

                var message = new TransportMessage(transportOperation.MessageId, transportOperation.Headers)
                {
                    Body = transportOperation.Body
                };

                //dispatch to transport

                if (transportOperation.Options["Operation"] != "Audit")
                {
                    DispatchMessageToTransportBehavior.InvokeNative(deliveryOptions, message);    
                }
                else
                {
                    DefaultMessageAuditer.Audit(deliveryOptions as SendOptions, message);
                }
                
            }
        }

      
    }
}
