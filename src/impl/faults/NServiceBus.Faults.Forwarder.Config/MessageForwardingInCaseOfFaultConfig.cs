using System.Configuration;

namespace NServiceBus.Config
{
    public class MessageForwardingInCaseOfFaultConfig : ConfigurationSection
    {
        /// <summary>
        /// The queue to which errors will be forwarded.
        /// </summary>
        [ConfigurationProperty("ErrorQueue", IsRequired = true)]
        public string ErrorQueue
        {
            get
            {
                return this["ErrorQueue"] as string;
            }
            set
            {
                this["ErrorQueue"] = value;
            }
        }
    }
}
