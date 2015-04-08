namespace NServiceBus
{
    using System;
    using NServiceBus.Outbox;
    using NServiceBus.Transports;
    using Pipeline.Contexts;

    class OutboxSendBehavior : PhysicalOutgoingContextStageBehavior
    {
        public DispatchMessageToTransportBehavior DispatchMessageToTransportBehavior { get; set; }

        public override void Invoke(Context context, Action next)
        {
            OutboxMessage currentOutboxMessage;

            if (context.TryGet(out currentOutboxMessage))
            {
                currentOutboxMessage.TransportOperations.Add( new TransportOperation(context.MessageId, context.DeliveryMessageOptions.ToTransportOperationOptions(), context.Body, context.Headers)); 
            }
            else
            {
                DispatchMessageToTransportBehavior.InvokeNative(context.DeliveryMessageOptions, new OutgoingMessage(context.MessageId,context.Headers,context.Body));

                next();
            }
        }
    }
}