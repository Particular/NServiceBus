using System.Configuration;

namespace NServiceBus.Config
{
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
