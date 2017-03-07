namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// Logging ConfigurationSection.
    /// </summary>
    [ObsoleteEx(
        Message = "Logging configuration via configuration section is discouraged.",
        ReplacementTypeOrMember = "LogManager.Use<DefaultFactory>()",
        TreatAsErrorFromVersion = "7",
        RemoveInVersion = "8")]
    public class Logging : ConfigurationSection
    {
        /// <summary>
        /// The minimal logging level above which all calls to the log will be written.
        /// </summary>
        [ConfigurationProperty("Threshold", IsRequired = true, DefaultValue = "Info")]
        [ObsoleteEx(
            Message = "Logging configuration via configuration section is discouraged.",
            ReplacementTypeOrMember = "LogManager.Use<DefaultFactory>().Level(LogLevel)",
            TreatAsErrorFromVersion = "7",
            RemoveInVersion = "8")]
        public string Threshold
        {
            get { return this["Threshold"] as string; }
            set { this["Threshold"] = value; }
        }
    }
}