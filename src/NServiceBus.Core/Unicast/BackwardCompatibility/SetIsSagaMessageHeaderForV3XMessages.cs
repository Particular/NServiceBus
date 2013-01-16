namespace NServiceBus.Unicast.BackwardCompatibility
{
    using MessageMutator;
    using Timeout;

    public class SetIsSagaMessageHeaderForV3XMessages : IMutateIncomingTransportMessages, INeedInitialization
    {
        public void MutateIncoming(TransportMessage transportMessage)
        {
            if (transportMessage.Headers.ContainsKey(Headers.IsSagaTimeoutMessage))
                return;

            if (transportMessage.Headers.ContainsKey(TimeoutManagerHeaders.Expire) &&
                transportMessage.Headers.ContainsKey(Headers.SagaId))
                transportMessage.Headers[Headers.IsSagaTimeoutMessage] = true.ToString();
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<SetIsSagaMessageHeaderForV3XMessages>(DependencyLifecycle.InstancePerCall);
        }
    }
}