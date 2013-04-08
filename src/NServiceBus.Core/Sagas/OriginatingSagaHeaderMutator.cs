namespace NServiceBus.Sagas
{
    using NServiceBus.MessageMutator;

    /// <summary>
    /// Adds the originating saga headers to outgoing messages
    /// </summary>
    public class OriginatingSagaHeaderMutator : IMutateOutgoingTransportMessages, INeedInitialization
    {
        /// <summary>
        /// Set the header if we run in the context of a saga
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="transportMessage"></param>
        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            if (SagaContext.Current == null)
                return;

            transportMessage.Headers[Headers.OriginatingSagaId] = SagaContext.Current.Entity.Id.ToString();
            transportMessage.Headers[Headers.OriginatingSagaType] = SagaContext.Current.GetType().AssemblyQualifiedName;
        }

        public void Init()
        {
            NServiceBus.Configure.Instance.Configurer
                .ConfigureComponent<OriginatingSagaHeaderMutator>(DependencyLifecycle.InstancePerCall);
        }
    }
}