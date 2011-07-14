using System.Configuration;

namespace NServiceBus.Config
{
    /// <summary>
    /// Configuration section for holding the node which is the master.
    /// </summary>
    public class MasterNodeLocatorConfig : ConfigurationSection
    {
        /// <summary>
        /// The node .
        /// </summary>
        [ConfigurationProperty("Node", IsRequired = true)]
        public string Node
        {
            get
            {
                return this["Node"] as string;
            }
            set
            {
                this["Node"] = value;
            }
        }
    }
}
