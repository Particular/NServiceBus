namespace NServiceBus.Routing
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents different ways how the transport should route a given message.
    /// </summary>
    public abstract class AddressTag
    {
        Dictionary<string, string> extensionData;

        /// <summary>
        /// Creates a new address tag.
        /// </summary>
        /// <param name="extensionData">Extension data.</param>
        protected AddressTag(Dictionary<string, string> extensionData)
        {
            this.extensionData = extensionData;
        }

        /// <summary>
        /// Returns an extension value for the specified key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <returns>True, if a given key/value was present.</returns>
        public bool TryGet(string key, out string value)
        {
            return extensionData.TryGetValue(key, out value);
        }

        internal IEnumerable<KeyValuePair<string, string>> GetExtensionValues()
        {
            return extensionData;
        } 
    }
}