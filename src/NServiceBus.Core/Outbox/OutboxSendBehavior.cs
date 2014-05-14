namespace NServiceBus.Outbox
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast.Behaviors;

    class OutboxSendBehavior : IBehavior<OutgoingContext>
    {
        public IOutboxStorage OutboxStorage { get; set; }

        public DispatchMessageToTransportBehavior DispatchMessageToTransportBehavior { get; set; }

        public void Invoke(OutgoingContext context, Action next)
        {
            OutboxMessage currentOutboxMessage;

            if (context.TryGet(out currentOutboxMessage) && !context.Get<bool>("Outbox_StartDispatching"))
            {
                currentOutboxMessage.TransportOperations.Add(new TransportOperation(context.SendOptions, context.OutgoingMessage, context.OutgoingLogicalMessage.MessageType.FullName));
            }
            else
            {
                DispatchMessageToTransportBehavior.InvokeNative(context.SendOptions, context.OutgoingMessage, context.OutgoingLogicalMessage.Metadata);

                next();
            }
        }
    }
}