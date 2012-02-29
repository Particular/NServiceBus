namespace MyServer
{
    using NServiceBus;
    using NServiceBus.Config;
    using NServiceBus.MessageMutator;
    using NServiceBus.Unicast.Transport;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server,IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                .DefaultBuilder()
                //shows multi tennant operations of the sagas
                .MessageToDatabaseMappingConvention(context =>
                                                      {
                                                          if (context.Headers.ContainsKey("tennant"))
                                                              return context.Headers["tennant"];

                                                          return string.Empty;
                                                      })
                .RunTimeoutManager(); //will default to ravendb for storage
        }
    }

    /// <summary>
    /// This mutator makes sure that the tennant id is propagated to all outgoing messages
    /// </summary>
    public class TennantPropagatingMutator : IMutateOutgoingTransportMessages, INeedInitialization
    {
        public IBus Bus { get; set; }

        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            if (Bus.CurrentMessageContext == null)
                return;
            if (!Bus.CurrentMessageContext.Headers.ContainsKey("tennant"))
                return;

            transportMessage.Headers["tennant"] = Bus.CurrentMessageContext.Headers["tennant"];
        }

        public void Init()
        {

            Configure.Instance.Configurer.ConfigureComponent<TennantPropagatingMutator>(
                DependencyLifecycle.InstancePerCall);
        }
    }
}
