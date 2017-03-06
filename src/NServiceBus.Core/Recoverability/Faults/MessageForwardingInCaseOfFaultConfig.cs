namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// Message Forwarding In Case Of Fault Config.
    /// </summary>
    [ObsoleteEx(
        Message = "Error queue configuration via configuration section is discouraged.",
        ReplacementTypeOrMember = "EndpointConfiguration.SendFailedMessagesTo",
        TreatAsErrorFromVersion = "7",
        RemoveInVersion = "8")]
    public class MessageForwardingInCaseOfFaultConfig : ConfigurationSection
    {
        /// <summary>
        /// The queue to which errors will be forwarded.
        /// </summary>
        [ConfigurationProperty("ErrorQueue", IsRequired = true)]
        [ObsoleteEx(
            Message = "Error queue configuration via configuration section is discouraged.",
            ReplacementTypeOrMember = "EndpointConfiguration.SendFailedMessagesTo",
            TreatAsErrorFromVersion = "7",
            RemoveInVersion = "8")]
        public string ErrorQueue
        {
            get { return this["ErrorQueue"] as string; }
            set { this["ErrorQueue"] = value; }
        }
    }
}