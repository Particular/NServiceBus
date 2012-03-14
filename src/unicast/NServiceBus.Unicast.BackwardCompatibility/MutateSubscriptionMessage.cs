using NServiceBus.Config;
using NServiceBus.MessageMutator;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Unicast.BackwardCompatibility 
{
    class MutateSubscriptionMessage : IMutateIncomingTransportMessages, INeedInitialization
    {
        /// <summary>
        /// Mutate incoming version 2.6 subscription messages.
        /// If the message is a completion message that contains the "EnclosedMessageTypes" key, then change the Message Intent.
        /// </summary>
        /// <param name="transportMessage"></param>
        public void MutateIncoming(TransportMessage transportMessage)
        {
            if (!transportMessage.Headers.ContainsKey("EnclosedMessageTypes"))
                return;

            transportMessage.MessageIntent--;
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<MutateSubscriptionMessage>(DependencyLifecycle.InstancePerCall);
        }
    }
}
