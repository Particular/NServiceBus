namespace NServiceBus.Sagas.Impl
{
    using MessageMutator;
    using Saga;

    /// <summary>
    /// Adds the saga id as a header to outgoing messages
    /// </summary>
    public class SagaIdEnricher:IMutateOutgoingMessages
    {
        /// <summary>
        /// Performs the mutation
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public object MutateOutgoing(object message)
        {
            var sagaMessage = message as ISagaMessage;

            if (sagaMessage != null)
                message.SetHeader(Headers.SagaId, sagaMessage.SagaId.ToString());

            return message;
        }
    }
}