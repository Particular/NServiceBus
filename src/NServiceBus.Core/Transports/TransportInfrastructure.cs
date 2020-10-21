using System;
using System.Linq;
using Janitor;
using NServiceBus.Settings;

namespace NServiceBus.Transport
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Transport infrastructure definitions.
    /// </summary>
    [SkipWeaving]
    public abstract class TransportInfrastructure : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public virtual IDispatchMessages Dispatcher { get; protected set; }

        /// <summary>
        /// </summary>
        public virtual IPushMessages[] Receivers { get; protected set; }

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
        public IPushMessages FindReceiver(string receiverId)
        {
            return Receivers.SingleOrDefault(r => r.Id == receiverId);
        }

        /// <summary>
        /// </summary>
        public abstract void Dispose();
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
        public string Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ReceiveAddress { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public PushSettings settings { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool UsePublishSubscribe { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool PurgeOnStartup { get; set; }
    }
}