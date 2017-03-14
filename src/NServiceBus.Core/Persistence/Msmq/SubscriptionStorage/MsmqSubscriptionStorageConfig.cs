namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// Contains the properties representing the MsmqSubscriptionStorage configuration section.
    /// </summary>
    [ObsoleteEx(
        Message = "MSMQ subscription storage configuration via configuration section is discouraged.",
        ReplacementTypeOrMember = "EndpointConfiguration.UsePersistence<MsmqPersistence>()",
        TreatAsErrorFromVersion = "7",
        RemoveInVersion = "8")]
    public class MsmqSubscriptionStorageConfig : ConfigurationSection
    {
        /// <summary>
        /// The queue where subscription data will be stored.
        /// Use the "queue@machine" convention.
        /// </summary>
        [ConfigurationProperty("Queue", IsRequired = true)]
        [ObsoleteEx(
        Message = "MSMQ subscription storage configuration via configuration section is discouraged.",
            ReplacementTypeOrMember = "EndpointConfiguration.UsePersistence<MsmqPersistence>().SubscriptionQueue",
            TreatAsErrorFromVersion = "7",
            RemoveInVersion = "8")]
        public string Queue
        {
            get { return this["Queue"] as string; }
            set { this["Queue"] = value; }
        }
    }
}