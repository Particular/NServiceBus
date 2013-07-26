namespace NServiceBus.Unicast.Monitoring
{
    using MessageMutator;

    /// <summary>
    /// Keeps track of the endpoint version to be used in side by side hosting
    /// </summary>
    public class EndpointVersionMutator : IMutateOutgoingTransportMessages, INeedInitialization
    {
        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            transportMessage.Headers[Headers.OriginatingEndpointVersion] = Configure.DefineEndpointVersionRetriever();
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<EndpointVersionMutator>(DependencyLifecycle.SingleInstance);
        }
    }
}