namespace NServiceBus.Outbox
{
    using System;
    using System.Collections.Generic;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast.Behaviors;
    using Unicast.Messages;

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
