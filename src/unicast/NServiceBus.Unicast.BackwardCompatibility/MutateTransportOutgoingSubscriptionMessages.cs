using System.IO;
using System.Threading;
using Common.Logging;
using NServiceBus.Config;
using NServiceBus.MessageMutator;
using NServiceBus.Serialization;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Unicast.BackwardCompatibility
{
    /// <summary>
    /// Allow for a V3.X subscriber to subscribe/unsubscribe to a V2.6 publisher
    /// </summary>
    public class MutateTransportOutgoingSubscriptionMessages : IMutateOutgoingTransportMessages, INeedInitialization
    {
        /// <summary>
        /// Allow for a V3.X subscriber to subscribe/unsubscribe to a V2.6 publisher
        /// Mutate outgoing subscribe/unsubscribe messages: Create and serialize a completion message into the 
        /// body of the transport message.
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="transportMessage"></param>
        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            if ((transportMessage.IsControlMessage() && 
                ((transportMessage.MessageIntent == MessageIntentEnum.Subscribe) || (transportMessage.MessageIntent == MessageIntentEnum.Unsubscribe))))
            {
                var stream = new MemoryStream();
                MessageSerializer.Serialize(new object[]  { new CompletionMessage() }, stream);
                transportMessage.Body = stream.ToArray();
                Log.Debug("Added Completion message and sending message intent: " + transportMessage.MessageIntent);
            }
        }

        /// <summary>
        /// Register the MutateTransportOutgoingSubscriptionMessages mutator
        /// </summary>
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<MutateTransportOutgoingSubscriptionMessages>(DependencyLifecycle.InstancePerCall);
        }
        /// <summary>
        /// Gets or sets the message serializer
        /// </summary>
        public IMessageSerializer MessageSerializer { get; set; }
        private readonly static ILog Log = LogManager.GetLogger(typeof(MutateTransportOutgoingSubscriptionMessages));
    }
}
