namespace NServiceBus.Unicast.Monitoring
{
    using MessageMutator;

    /// <summary>
    /// Keeps track of the endpoint version to be used in side by side hosting
    /// </summary>
    public class EndPointVersionMutator : IMutateOutgoingTransportMessages, INeedInitialization
    {
        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            transportMessage.Headers[Headers.OriginatingEndPointVersion] = Configure.DefineEndpointVersionRetriever();
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<EndPointVersionMutator>(DependencyLifecycle.SingleInstance);
        }
    }
}