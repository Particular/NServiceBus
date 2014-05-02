namespace NServiceBus.Config
{
    using System.Configuration;

    public class ChannelCollection : ConfigurationElementCollection
    {
        /// <summary>
        /// Creates a new empty property
        /// </summary>
        protected override ConfigurationElement CreateNewElement()
        {
            return new ChannelConfig();
        }

        /// <summary>
        /// Returns the key for the given element
        /// </summary>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ChannelConfig) element).Address;
        }

        public override bool IsReadOnly()
        {
            return false;
        }

        /// <summary>
        /// Calls BaseAdd.
        /// </summary>
        public void Add(ChannelConfig channel)
        {
            BaseAdd(channel);
        }

        /// <summary>
        /// Calls BaseAdd with true as the additional parameter.
        /// </summary>
        protected override void BaseAdd(ConfigurationElement element)
        {
            BaseAdd(element, true);
        }
    }
}