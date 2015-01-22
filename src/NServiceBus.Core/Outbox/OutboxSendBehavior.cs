namespace NServiceBus
{
    using System;
    using NServiceBus.Outbox;
    using Pipeline.Contexts;

    class OutboxSendBehavior : PhysicalOutgoingContextStageBehavior
    {
        public DispatchMessageToTransportBehavior DispatchMessageToTransportBehavior { get; set; }

        public override void Invoke(Context context, Action next)
        {
            OutboxMessage currentOutboxMessage;

            if (context.TryGet(out currentOutboxMessage))
            {
                currentOutboxMessage.TransportOperations.Add( new TransportOperation(context.OutgoingMessage.Id, context.DeliveryOptions.ToTransportOperationOptions(), context.OutgoingMessage.Body, context.OutgoingMessage.Headers)); 
            }
            else
            {
                DispatchMessageToTransportBehavior.InvokeNative(context.DeliveryOptions, context.OutgoingMessage);

                next();
            }
        }
    }
}