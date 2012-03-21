using Common.Logging;
using NServiceBus.Config;
using NServiceBus.MessageMutator;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Unicast.BackwardCompatibility 
{
    class MutateTransportIncomingSubscriptionMessages : IMutateIncomingTransportMessages, INeedInitialization
    {
        /// <summary>
        /// Re-Adjust V3.0.0 subscribe & UnSubscribe messages. Version 3.0.0 Subs/Unsubs/Publish  no NServiceBus.Version set it the headers.
        /// Do nothing If it is  a V2.6 message (contains EnclosedMessageTypes key).
        /// </summary>
        /// <param name="transportMessage"></param>
        public void MutateIncoming(TransportMessage transportMessage)
        {
            if ((!transportMessage.Headers.ContainsKey(Headers.NServiceBusVersion) || 
                (transportMessage.Headers.ContainsKey(Headers.NServiceBusVersion)) && (transportMessage.Headers[Headers.NServiceBusVersion] == "3.0.0")) && 
                (!transportMessage.Headers.ContainsKey("EnclosedMessageTypes")))
            {
                transportMessage.MessageIntent++;
                Log.Debug("Just mutated V3.0.0 to message intent: " + transportMessage.MessageIntent);
            }
        }

        /// <summary>
        /// Register the MutateTransportIncomingSubscriptionMessages mutator
        /// </summary>
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<MutateTransportIncomingSubscriptionMessages>(DependencyLifecycle.InstancePerCall);
            Log.Debug("Configured Transport Incoming Message Mutator: MutateTransportIncomingSubscriptionMessages");
        }

        private readonly static ILog Log = LogManager.GetLogger(typeof(MutateTransportIncomingSubscriptionMessages));
    }
}
