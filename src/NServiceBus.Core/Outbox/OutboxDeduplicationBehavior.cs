namespace NServiceBus.Outbox
{
    using System;
    using System.Collections.Generic;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast.Behaviors;
    using Unicast.Messages;

    public class OutboxDeduplicationBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        public IOutboxStorage OutboxStorage { get; set; }
        public DispatchMessageToTransportBehavior DispatchMessageToTransportBehavior { get; set; }

        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }

        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            var messageId = context.PhysicalMessage.Id;
            OutboxMessage outboxMessage;

            if (!OutboxStorage.TryGet(messageId, out outboxMessage))
            {
                outboxMessage = new OutboxMessage(messageId);

                context.Set(outboxMessage);

                next();
            }

            if (outboxMessage.Dispatched)
            {
                return;
            }

            context.Set("Outbox_StartDispatching", true);

            DispatchOperationToTransport(outboxMessage.TransportOperations);

            OutboxStorage.SetAsDispatched(messageId);
        }

        void DispatchOperationToTransport(IEnumerable<TransportOperation> operations)
        {
            foreach (var transportOperation in operations)
            {
                var metadata = MessageMetadataRegistry.GetMessageMetadata(transportOperation.MessageType);

                //dispatch to transport
                DispatchMessageToTransportBehavior.InvokeNative(transportOperation.SendOptions, transportOperation.Message, metadata);
            }
        }
    }
}
