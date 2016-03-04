namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// Logging ConfigurationSection.
    /// </summary>
    public class Logging : ConfigurationSection
    {
        /// <summary>
        /// The minimal logging level above which all calls to the log will be written.
        /// </summary>
        [ConfigurationProperty("Threshold", IsRequired = true, DefaultValue = "Info")]
        public string Threshold
        {
            get { return this["Threshold"] as string; }
            set { this["Threshold"] = value; }
        }
    }
}