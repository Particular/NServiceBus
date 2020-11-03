using System;
using System.Collections.ObjectModel;
using System.Linq;
using Janitor;
using NServiceBus.Settings;
using NServiceBus.Unicast.Messages;

namespace NServiceBus.Transport
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Transport infrastructure definitions.
    /// </summary>
    [SkipWeaving]
    public abstract class TransportInfrastructure : IAsyncDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public virtual IMessageDispatcher Dispatcher { get; protected set; }

        /// <summary>
        /// </summary>
        public virtual ReadOnlyCollection<IMessageReceiver> Receivers { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public virtual Task ValidateNServiceBusSettings(ReadOnlySettings settings)
        {
            // this is only called when the transport is hosted as part of NServiceBus. No need to call this as "raw users".
            // pass a settings type that only allows "tryGet".
            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        public IMessageReceiver GetReceiver(string receiverId)
        {
            return Receivers.SingleOrDefault(r => r.Id == receiverId);
        }


        /// <summary>
        /// </summary>
        public abstract ValueTask DisposeAsync();
    }

    /// <summary>
    /// 
    /// </summary>
    public class EndpointAddress
    {
        /// <summary>
        /// 
        /// </summary>
        public string Endpoint { get; }

        /// <summary>
        /// A specific discriminator for scale-out purposes.
        /// </summary>
        public string Discriminator { get; }

        /// <summary>
        /// Returns all the differentiating properties of this instance.
        /// </summary>
        public IReadOnlyDictionary<string, string> Properties { get; }

        /// <summary>
        /// 
        /// </summary>
        public string Qualifier { get; }

        /// <summary>
        /// 
        /// </summary>
        public EndpointAddress(string endpoint, string discriminator, IReadOnlyDictionary<string, string> properties,
            string qualifier)
        {
            Endpoint = endpoint;
            Discriminator = discriminator;
            Properties = properties;
            Qualifier = qualifier;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ReceiveSettings
    {
        /// <summary>
        /// </summary>
        public ReceiveSettings(string id, string receiveAddress, bool usePublishSubscribe, bool purgeOnStartup, string errorQueue, TransportTransactionMode requiredTransactionMode, IReadOnlyCollection<MessageMetadata> events)
        {
            Id = id;
            ReceiveAddress = receiveAddress;
            UsePublishSubscribe = usePublishSubscribe;
            PurgeOnStartup = purgeOnStartup;
            ErrorQueue = errorQueue;
            RequiredTransactionMode = requiredTransactionMode;
            Events = events;
        }

        /// <summary>
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ReceiveAddress { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool UsePublishSubscribe { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool PurgeOnStartup { get; set; }

        /// <summary>
        /// The native queue where to send corrupted messages to.
        /// </summary>
        public string ErrorQueue { get; }

        /// <summary>
        /// The transaction mode required for receive operations.
        /// </summary>
        public TransportTransactionMode RequiredTransactionMode { get; }

        /// <summary>
        /// A list of events that this endpoint is handling.
        /// </summary>
        public IReadOnlyCollection<MessageMetadata> Events { get; }
    }
}