namespace NServiceBus
{
    using System;
    using NServiceBus.Outbox;
    using Pipeline;
    using Pipeline.Contexts;

    class OutboxSendBehavior : IBehavior<OutgoingContext>
    {
        public DispatchMessageToTransportBehavior DispatchMessageToTransportBehavior { get; set; }

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

                if (!context.OutgoingMessage.Recoverable)
                {
                    options["NonDurable"] = bool.TrueString;
                }

                currentOutboxMessage.TransportOperations.Add(transportOperation);
            }
            else
            {
                DispatchMessageToTransportBehavior.InvokeNative(context.DeliveryOptions, context.OutgoingMessage);

                next();
            }
        }
    }
}