namespace NServiceBus.Outbox
{
    using Pipeline;
    using Transports;
    using Unicast;

    class OutboxAwareAuditer
    {
        public DefaultMessageAuditer DefaultMessageAuditer { get; set; }

        public PipelineExecutor PipelineExecutor { get; set; }

        public void Audit( SendOptions sendOptions, TransportMessage message)
        {
            var context = PipelineExecutor.CurrentContext;

            OutboxMessage currentOutboxMessage;

            if (context.TryGet(out currentOutboxMessage))
            {
                message.RevertToOriginalBodyIfNeeded();
                
                currentOutboxMessage.TransportOperations.Add(new TransportOperation(message.Id, sendOptions.ToTransportOperationOptions(true), message.Body, message.Headers));
            }
            else
            {
                DefaultMessageAuditer.Audit(sendOptions, message);
            }
        }
    }
}