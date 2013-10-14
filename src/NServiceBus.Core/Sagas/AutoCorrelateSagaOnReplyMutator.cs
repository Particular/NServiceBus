namespace NServiceBus.Sagas
{
    using System;
    using MessageMutator;

    /// <summary>
    /// Promotes the saga id and type headers on replies(bus.Reply|bus.Return) so that the saga can be 
    /// correlated without the user having to add mappings for it. This replaces the ISagaMessage feature
    /// </summary>
    public class AutoCorrelateSagaOnReplyMutator : IMutateTransportMessages, INeedInitialization
    {
        /// <summary>
        /// Stores the original saga id and type of the incoming message
        /// </summary>
        /// <param name="transportMessage"></param>
        public void MutateIncoming(TransportMessage transportMessage)
        {
            originatingSagaId = null;
            originatingSagaType = null;

            // We need this for backwards compatibility because in v4.0.0 we still have this headers being sent as part of the message even if MessageIntent == MessageIntentEnum.Publish
            if (transportMessage.MessageIntent == MessageIntentEnum.Publish)
            {
                transportMessage.Headers.Remove(Headers.SagaId);
                transportMessage.Headers.Remove(Headers.SagaType);
            }

            if (transportMessage.Headers.ContainsKey(Headers.OriginatingSagaId))
            {
                originatingSagaId = transportMessage.Headers[Headers.OriginatingSagaId];
            }

            if (transportMessage.Headers.ContainsKey(Headers.OriginatingSagaType))
            {
                originatingSagaType = transportMessage.Headers[Headers.OriginatingSagaType];
            }

        }
        
        /// <summary>
        /// Promotes the id and type of the originating saga if it is a reply
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="transportMessage"></param>
        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            if (transportMessage.MessageIntent != MessageIntentEnum.Reply)
            {
                return;
            }

            //for now we revert back to send since this would be a breaking change. We'll fix this in v4.1
            //https://github.com/NServiceBus/NServiceBus/issues/1409
            transportMessage.MessageIntent = MessageIntentEnum.Send;
            

            if (string.IsNullOrEmpty(originatingSagaId))
            {
                return;
            }

            transportMessage.Headers[Headers.SagaId] = originatingSagaId;

            //we do this check for backwards compatibility since older versions on NSB can set the saga id but not the type
            if (!string.IsNullOrEmpty(originatingSagaType))
            {
                transportMessage.Headers[Headers.SagaType] = originatingSagaType;
            }
        }

        public void Init()
        {
            Configure.Instance.Configurer
                .ConfigureComponent<AutoCorrelateSagaOnReplyMutator>(DependencyLifecycle.InstancePerCall);
        }

        [ThreadStatic]
        static string originatingSagaId;


        [ThreadStatic]
        static string originatingSagaType;
    }
}