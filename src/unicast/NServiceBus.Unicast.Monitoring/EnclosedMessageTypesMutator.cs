namespace NServiceBus.Unicast.Monitoring
{
    using System.Collections.Generic;
    using System.Linq;
    using MessageMutator;

    /// <summary>
    /// Adds a header with the types of the messages enclosed in this transport message
    /// </summary>
    public class EnclosedMessageTypesMutator : IMutateOutgoingTransportMessages, INeedInitialization
    {
        /// <summary>
        /// Sets the header to a ; separated list of message types carried in this message
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="transportMessage"></param>
        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            if(messages.Any())
                transportMessage.Headers[Headers.EnclosedMessageTypes] = SerializeEnclosedMessageTypes(messages);
        }

        static string SerializeEnclosedMessageTypes(IEnumerable<object> messages)
        {
            var types = messages.Select(m => m.GetType()).ToList();

            var interfaces = types.SelectMany(t => t.GetInterfaces())
                .Where(t => t.IsMessageType());

            var noneProxyTypes = types.Distinct().Where(t => !t.Assembly.IsDynamic); // Exclude proxies
            var interfacesOrderedByHierarchy = interfaces.Distinct().OrderByDescending(i => i.GetInterfaces().Count()); // Interfaced less interfaces are lower in the hierarchy. 

            return string.Join(";", noneProxyTypes.Concat(interfacesOrderedByHierarchy).Select(t => t.AssemblyQualifiedName));
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