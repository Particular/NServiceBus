namespace NServiceBus.Outbox
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast.Behaviors;
    using Unicast.Messages;

    class OutboxSendBehavior : IBehavior<SendPhysicalMessageContext>
    {
        public IOutboxStorage OutboxStorage { get; set; }

        public void Invoke(SendPhysicalMessageContext context, Action next)
        {
            OutboxMessage currentOutboxMessage;

            if (context.TryGet(out currentOutboxMessage) && !context.Get<bool>("Outbox_StartDispatching"))
            {
                currentOutboxMessage.TransportOperations.Add(new TransportOperation(context.SendOptions, context.MessageToSend,context.LogicalMessage.MessageType.FullName));
            }
            else
            {
                next();
            }
        }


    }
}