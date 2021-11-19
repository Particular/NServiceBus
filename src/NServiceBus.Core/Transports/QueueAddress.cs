namespace NServiceBus.Transport
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text;

    /// <summary>
    /// Represents a queue address.
    /// </summary>
    public class QueueAddress
    {
        static readonly IReadOnlyDictionary<string, string> EmptyProperties = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(0));

        /// <summary>
        /// Creates a new instance of <see cref="QueueAddress"/>.
        /// </summary>
        public QueueAddress(
            string baseAddress,
            string discriminator = null,
            IReadOnlyDictionary<string, string> properties = null,
            string qualifier = null)
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

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            if (Qualifier != null)
            {
                sb.Append(Qualifier).Append('.');
            }

            sb.Append(BaseAddress);

            if (Discriminator != null)
            {
                sb.Append('-').Append(Discriminator);
            }

            if (Properties.Count > 0)
            {
                sb.Append('(');
                foreach (var property in Properties)
                {
                    sb.Append(property.Key).Append(':').Append(property.Value).Append(';');
                }

                sb.Length--; // trim last ;
                sb.Append(')');
            }

            return sb.ToString();
        }
    }
}