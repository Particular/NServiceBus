using System.Configuration;

namespace NServiceBus.Config
{
    /// <summary>
    /// Contains the properties representing the MsmqSubscriptionStorage configuration section.
    /// </summary>
    public class MsmqSubscriptionStorageConfig : ConfigurationSection
    {
        [ConfigurationProperty("Queue", IsRequired = true)]
        public string Queue
        {
            get
            {
                return this["Queue"] as string;
            }
            set
            {
                this["Queue"] = value;
            }
        }
    }
}
