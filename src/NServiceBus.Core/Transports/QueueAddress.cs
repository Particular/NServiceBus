namespace NServiceBus.Transport
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Represents a queue address.
    /// </summary>
    public class QueueAddress
    {
        static readonly IReadOnlyDictionary<string, string> EmptyProperties = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(0));

        /// <summary>
        /// Creates a new instance of <see cref="QueueAddress"/>.
        /// </summary>
        public QueueAddress(string baseAddress, string discriminator, IReadOnlyDictionary<string, string> properties,
            string qualifier)
        {
            BaseAddress = baseAddress;
            Discriminator = discriminator;
            Properties = properties ?? EmptyProperties;
            Qualifier = qualifier;
        }

        /// <summary>
        /// A queue name without transport specific properties.
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
        /// An additional identifier for logical "sub-queues".
        /// </summary>
        public string Qualifier { get; }
    }
}