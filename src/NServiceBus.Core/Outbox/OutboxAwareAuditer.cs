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

        public void Audit( SendMessageOptions sendMessageOptions, OutgoingMessage message)
        {
            OutboxMessage currentOutboxMessage;

            if (behaviorContext.TryGet(out currentOutboxMessage))
            {    
                currentOutboxMessage.TransportOperations.Add(new TransportOperation(message.MessageId, sendMessageOptions.ToTransportOperationOptions(true), message.Body, message.Headers));
            }
            else
            {
                defaultMessageAuditer.Audit(new TransportSendOptions(sendMessageOptions.Destination), message);
            }
        }
    }
}