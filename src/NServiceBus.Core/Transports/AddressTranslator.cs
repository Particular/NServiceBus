namespace NServiceBus.Transports
{
    /// <summary>
    /// Translates <see cref="Address"/> to a string that can be used by the dynamic routing to do lookups for physical addresses.
    /// </summary>
    public class AddressTranslator
    {
        /// <summary>
        /// Translates <see cref="Address"/> to a string that can be used by the dynamic routing to do lookups for physical addresses.
        /// </summary>
        /// <param name="address">The address to translate.</param>
        /// <returns>The string to use for lookup.</returns>
        public virtual string Translate(Address address)
        {
            return address.ToString();
        }
    }
}