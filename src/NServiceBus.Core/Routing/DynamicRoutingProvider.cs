namespace NServiceBus.Routing
{
    using NServiceBus.Transports;

    /// <summary>
    /// Wraps both the <see cref="AddressTranslator"/> and <see cref="IProvideDynamicRouting"/> in a convenient injectable class.
    /// </summary>
    public class DynamicRoutingProvider
    {
        /// <summary>
        /// The registered <see cref="IProvideDynamicRouting"/> impl.
        /// </summary>
        public IProvideDynamicRouting DynamicRouting { get; set; }
        
        /// <summary>
        /// The registered <see cref="AddressTranslator"/> impl.
        /// </summary>
        public AddressTranslator Translator { get; set; }

        /// <summary>
        /// Returns the full address to send messages to based on the <paramref name="address"/> provided.
        /// If no routing distribution is available for the <paramref name="address"/>, the same address is returned.
        /// </summary>
        /// <param name="address">The logical endpoint address to get a dynamic address for.</param>
        /// <returns>The full address to send to.</returns>
        public Address GetRouteAddress(Address address)
        {
            if (DynamicRouting == null)
            {
                return address;
            }

            string s;
            var result = DynamicRouting.TryGetRouteAddress(Translator.Translate(address), out s);
            Address dynamicAddress;
            
            if (result)
            {
                dynamicAddress = Address.Parse(s);
            }
            else
            {
                dynamicAddress = address;
            }

            return dynamicAddress;
        }
    }
}