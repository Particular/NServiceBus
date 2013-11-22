namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class OutboxSendBehavior : IBehavior<SendPhysicalMessageContext>
    {
        public IOutboxStorage OutboxStorage { get; set; }

        public void Invoke(SendPhysicalMessageContext context, Action next)
        {
            OutboxMessage currentOutboxMessage;

            if (!context.TryGet(out currentOutboxMessage))
            {
                throw new InvalidOperationException("No current outbox message found");
            }

            if (currentOutboxMessage.IsDispatching)
            {
                next();
            }
            else
            {
                currentOutboxMessage.TransportOperations.Add(new TransportOperation(context.SendOptions, context.MessageToSend));                
            }
        }

    }
}