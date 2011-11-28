namespace NServiceBus.Timeout.Core
{
    using MessageMutator;
    using Saga;

    public class SagaIdRewriter : IMutateOutgoingMessages
    {
        public object MutateOutgoing(object message)
        {
            var sagaMessage = message as ISagaMessage;

            if (sagaMessage != null)
                message.SetHeader(Headers.SagaId, sagaMessage.SagaId.ToString());
            
            return message;
        }
    }
}