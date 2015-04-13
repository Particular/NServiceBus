namespace NServiceBus
{
    using System;
    using NServiceBus.Outbox;
    using NServiceBus.Pipeline.Contexts;

    class OutboxSendBehavior : PhysicalOutgoingContextStageBehavior
    {
        readonly DispatchMessageToTransportBehavior dispatchMessageToTransportBehavior;

        public OutboxSendBehavior(DispatchMessageToTransportBehavior dispatchMessageToTransportBehavior)
        {
            this.dispatchMessageToTransportBehavior = dispatchMessageToTransportBehavior;
        }

        public override void Invoke(Context context, Action next)
        {
            OutboxMessage currentOutboxMessage;

            if (context.TryGet(out currentOutboxMessage))
            {
                currentOutboxMessage.TransportOperations.Add( new TransportOperation(context.MessageId, context.DeliveryMessageOptions.ToTransportOperationOptions(), context.Body, context.Headers)); 
            }
            else
            {
                dispatchMessageToTransportBehavior.InvokeNative(context);

                next();
            }
        }
    }
}