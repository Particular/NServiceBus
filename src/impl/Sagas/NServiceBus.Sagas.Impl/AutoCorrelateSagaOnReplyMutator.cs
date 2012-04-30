namespace NServiceBus.Sagas.Impl
{
    using System;
    using Config;
    using MessageMutator;
    using Unicast.Transport;

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

            if (transportMessage.Headers.ContainsKey(Headers.OriginatingSagaId))
                originatingSagaId = transportMessage.Headers[Headers.OriginatingSagaId];

            if (transportMessage.Headers.ContainsKey(Headers.OriginatingSagaType))
                originatingSagaType = transportMessage.Headers[Headers.OriginatingSagaType];

        }
        
        /// <summary>
        /// Promotes the id and type of the originating saga if the is a reply
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="transportMessage"></param>
        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            //if correlation id is not set this is not a replay so we can just return
            if (string.IsNullOrEmpty(transportMessage.CorrelationId))
                return;

            if(string.IsNullOrEmpty(originatingSagaId))
                return;

            transportMessage.Headers[Headers.SagaId] = originatingSagaId;

            //we do this check for bacwards compat since older versions on NSB can set the saga id but not the type
            if(!string.IsNullOrEmpty(originatingSagaType))
                transportMessage.Headers[Headers.SagaType] = originatingSagaType;
        }

        public void Init()
        {
            NServiceBus.Configure.Instance.Configurer
                .ConfigureComponent<AutoCorrelateSagaOnReplyMutator>(DependencyLifecycle.InstancePerCall);
        }

        [ThreadStatic]
        static string originatingSagaId;


        [ThreadStatic]
        static string originatingSagaType;
    }
}