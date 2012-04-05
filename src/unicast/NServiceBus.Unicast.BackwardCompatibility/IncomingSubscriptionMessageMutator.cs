using NServiceBus.Logging;
using NServiceBus.Config;
using NServiceBus.MessageMutator;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Unicast.BackwardCompatibility 
{
    class IncomingSubscriptionMessageMutator : IMutateIncomingTransportMessages, INeedInitialization
    {
        /// <summary>
        /// Re-Adjust V3.0.0 subscribe and  unsubscribe messages. 
        /// Version 3.0.0 subscribe and unsubscribe message have no NServiceBus.Version set it the headers.
        /// Version 3.0.0 Send message have it with "3.0.0" set as value.
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
        /// Register the IncomingSubscriptionMessageMutator mutator
        /// </summary>
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<IncomingSubscriptionMessageMutator>(DependencyLifecycle.InstancePerCall);
            Log.Debug("Configured Transport Incoming Message Mutator: IncomingSubscriptionMessageMutator");
        }

        private readonly static ILog Log = LogManager.GetLogger(typeof(IncomingSubscriptionMessageMutator));
    }
}
