using System.Configuration;

namespace NServiceBus.Config
{
    /// <summary>
    /// Contains the properties representing the MsmqSubscriptionStorage configuration section.
    /// </summary>
    public class MsmqSubscriptionStorageConfig : ConfigurationSection
    {
        /// <summary>
        /// The queue where subscription data will be stored.
        /// Use the "queue@machine" convention.
        /// </summary>
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
