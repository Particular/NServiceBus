using System.Collections.Generic;

namespace NServiceBus.Transport
{
    /// <summary>
    /// 
    /// </summary>
    public class QueueAddress
    {
        /// <summary>
        /// 
        /// </summary>
        public string BaseAddress { get; }

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
        public QueueAddress(string baseAddress, string discriminator, IReadOnlyDictionary<string, string> properties,
            string qualifier)
        {
            BaseAddress = baseAddress;
            Discriminator = discriminator;
            Properties = properties;
            Qualifier = qualifier;
        }
    }
}