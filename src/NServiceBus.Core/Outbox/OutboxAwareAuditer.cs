namespace NServiceBus.Outbox
{
    using Pipeline;
    using Transports;
    using Unicast;

    class OutboxAwareAuditer
    {
        readonly DefaultMessageAuditer defaultMessageAuditer;
        readonly BehaviorContext behaviorContext;

        public OutboxAwareAuditer(DefaultMessageAuditer defaultMessageAuditer, BehaviorContext behaviorContext)
        {
            this.defaultMessageAuditer = defaultMessageAuditer;
            this.behaviorContext = behaviorContext;
        }

        public void Audit( SendOptions sendOptions, OutgoingMessage message)
        {
            OutboxMessage currentOutboxMessage;

            if (behaviorContext.TryGet(out currentOutboxMessage))
            {    
                currentOutboxMessage.TransportOperations.Add(new TransportOperation(message.MessageId, sendOptions.ToTransportOperationOptions(true), message.Body, message.Headers));
            }
            else
            {
                defaultMessageAuditer.Audit(sendOptions, message);
            }
        }
    }
}