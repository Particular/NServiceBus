namespace NServiceBus
{
    /// <summary>
    /// Represents a logical address (independent of transport).
    /// </summary>
    public class LocalAddress
    {
        /// <summary>
        /// Creates new qualified local address for the provided endpoint instance name.
        /// </summary>
        /// <param name="instanceName">The name of the instance.</param>
        /// <param name="qualifier">The qualifier of this address.</param>
        /// <param name="discriminator">The discriminator of this address.</param>
        public LocalAddress(string instanceName, string qualifier = null, string discriminator = null)
        {
            Guard.AgainstNullAndEmpty(nameof(instanceName), instanceName);

            InstanceName = instanceName;
            Qualifier = qualifier;
            Discriminator = discriminator;
        }

        /// <summary>
        /// Returns the qualifier or null for the local endpoint.
        /// </summary>
        public string Qualifier { get; }

        /// <summary>
        /// Returns the discriminator or null for the local endpoint.
        /// </summary>
        public string Discriminator { get; }

        /// <summary>
        /// Returns the instance name.
        /// </summary>
        public string InstanceName { get; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            var value = InstanceName;
            if (Discriminator != null)
            {
                value += "-" + Discriminator;
            }
            if (Qualifier != null)
            {
                value += "." + Qualifier;
            }
            return value;
        }
    }
}