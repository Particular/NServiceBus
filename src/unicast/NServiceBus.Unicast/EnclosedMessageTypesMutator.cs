namespace NServiceBus.Unicast
{
    using System.Collections.Generic;
    using System.Linq;
    using Config;
    using MessageMutator;
    using Transport;

    /// <summary>
    /// Adds a header with the types of the messages enclosed in this transport message
    /// </summary>
    public class EnclosedMessageTypesMutator : IMutateOutgoingTransportMessages,INeedInitialization
    {
        /// <summary>
        /// Header entry key indicating the types of messages contained.
        /// </summary>
        public const string EnclosedMessageTypes = "NServiceBus.EnclosedMessageTypes";


        /// <summary>
        /// Sets the header to a ; separated list of message types carried in this message
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="transportMessage"></param>
        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            if(messages.Any())
                transportMessage.Headers[EnclosedMessageTypes] = SerializeEnclosedMessageTypes(messages);
        }

        static string SerializeEnclosedMessageTypes(IEnumerable<object> messages)
        {
            var types = messages.Select(m => m.GetType());
            
            var interfaces = types.SelectMany(t => t.GetInterfaces())
                .Where(t=>t.IsMessageType());
            
            return string.Join(";", types.Concat(interfaces).Distinct().Select(t=>t.AssemblyQualifiedName));
        }

        /// <summary>
        /// Initializes 
        /// </summary>
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<EnclosedMessageTypesMutator>(DependencyLifecycle.InstancePerCall);
        }
    }
}