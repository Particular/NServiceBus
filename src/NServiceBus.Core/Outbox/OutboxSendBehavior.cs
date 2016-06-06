namespace NServiceBus
{
    using System;
    using NServiceBus.Outbox;
    using Pipeline;
    using Pipeline.Contexts;

    class OutboxSendBehavior : IBehavior<OutgoingContext>
    {
        private DispatchMessageToTransportBehavior dispatchMessageToTransportBehavior;

        public OutboxSendBehavior(DispatchMessageToTransportBehavior dispatchMessageToTransportBehavior)
        {
            this.dispatchMessageToTransportBehavior = dispatchMessageToTransportBehavior;
        }

        public void Invoke(OutgoingContext context, Action next)
        {
            OutboxMessage currentOutboxMessage;

            if (context.TryGet(out currentOutboxMessage))
            {
                var options = context.DeliveryOptions.ToTransportOperationOptions();
                var transportOperation = new TransportOperation(context.OutgoingMessage.Id, options, context.OutgoingMessage.Body, context.OutgoingMessage.Headers);

                if (context.OutgoingMessage.TimeToBeReceived != TimeSpan.MaxValue)
                {
                    options["TimeToBeReceived"] = context.OutgoingMessage.TimeToBeReceived.ToString();
                }

                currentOutboxMessage.TransportOperations.Add(transportOperation);
            }
            else
            {
                dispatchMessageToTransportBehavior.InvokeNative(context.DeliveryOptions, context.OutgoingMessage);

                next();
            }
        }
    }
}