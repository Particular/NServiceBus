namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// Collection of sites
    /// </summary>
    public class SiteCollection : ConfigurationElementCollection
    {
        /// <summary>
        /// Creates a new empty property
        /// </summary>
        protected override ConfigurationElement CreateNewElement()
        {
            return new SiteConfig();
        }

        /// <summary>
        /// Returns the key for the given element
        /// </summary>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((SiteConfig) element).Key;
        }

        public override bool IsReadOnly()
        {
            return false;
        }

        /// <summary>
        /// Calls BaseAdd.
        /// </summary>
        public void Add(SiteConfig site)
        {
            BaseAdd(site);
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