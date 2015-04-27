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
                var options = sendMessageOptions.ToTransportOperationOptions();

                options["Operation"] = "Audit";

                currentOutboxMessage.TransportOperations.Add(new TransportOperation(message.MessageId,options, message.Body, message.Headers));
            }
            else
            {
                defaultMessageAuditer.Audit(new TransportSendOptions(sendMessageOptions.Destination), message);
            }
        }
    }
}