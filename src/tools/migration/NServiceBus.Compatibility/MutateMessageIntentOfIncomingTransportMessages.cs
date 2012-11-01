using System;
using System.Linq;

namespace NServiceBus.Compatibility
{
    using MessageMutator;

    public class MutateMessageIntentOfIncomingTransportMessages : IMutateIncomingTransportMessages, INeedInitialization
    {
        /// <summary>
        /// Make MessageIntent to be compatible between NServiceBus V3.0.0 and later versions.
        /// In publish/subscribe/unsubscribe in V3.0.0 there is no Version header.
        /// In send in V3.0.0 the header is set with 3.0.0 value.
        /// In both cases, do not mutate the MessageIntent.
        /// </summary>
        /// <param name="transportMessage">Transport Message to mutate.</param>
        public void MutateIncoming(TransportMessage transportMessage)
        {
            if (transportMessage.Headers.ContainsKey("NServiceBus.Version") && transportMessage.Headers["NServiceBus.Version"] != "3.0.0")
            {
                if ((int)transportMessage.MessageIntent == 0)
                    transportMessage.MessageIntent = Enum.GetValues(typeof(MessageIntentEnum)).Cast<MessageIntentEnum>().Max();
                else
                    transportMessage.MessageIntent--;
            }
        }

        /// <summary>
        /// Register the MutateTransportIncomingSubscriptionMessages mutator
        /// </summary>
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<MutateMessageIntentOfIncomingTransportMessages>(DependencyLifecycle.InstancePerCall);
        }
    }
}
