using NServiceBus.Config;
using NServiceBus.Grid.Messages;
using NServiceBus.MessageMutator;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Grid.MessageHandlers
{
    /// <summary>
    /// Bootstraps the behavior needed for a client to communicate with a distributor
    /// </summary>
    public class Bootstrapper : INeedInitialization, IMapOutgoingTransportMessages
    {
        void INeedInitialization.Init()
        {
            var readyConfig = Configure.Instance.Configurer.ConfigureComponent<ReadyManager>(ComponentCallModelEnum.Singleton);

            var cfg = Configure.GetConfigSection<UnicastBusConfig>();
            if (cfg != null)
            {
                readyConfig.ConfigureProperty(r => r.DistributorControlAddress, cfg.DistributorControlAddress);
                DistributorDataAddress = cfg.DistributorDataAddress;
            }

            Configure.ConfigurationComplete +=
                (o, e) => Configure.Instance.Builder.Build<ReadyManager>().SendReadyMessage(true);
        }

        void IMapOutgoingTransportMessages.MapOutgoing(IMessage[] messages, TransportMessage transportMessage)
        {
            if (messages[0] is ReadyMessage)
                return;

            //when not talking to the distributor, pretend that our address is that of the distributor
            transportMessage.ReturnAddress = DistributorDataAddress;
        }

        private string DistributorDataAddress { get; set; }
    }
}
