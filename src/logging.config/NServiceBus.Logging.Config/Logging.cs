using System.Configuration;

namespace NServiceBus.Config
{
    /// <summary>
    /// Logging ConfigurationSection
    /// </summary>
    public class Logging : ConfigurationSection
    {
        /// <summary>
        /// The minimal logging level above which all calls to the log will be written
        /// </summary>
        [ConfigurationProperty("Threshold", IsRequired = true)]
        public string Threshold
        {
            get
            {
                return this["Threshold"] as string;
            }
            set
            {
                this["Threshold"] = value;
            }
        }
    }
}
