#pragma warning disable 1591
namespace NServiceBus.Config
{
    using System.Configuration;

    [ObsoleteEx(Message = "Use NServiceBus/Transport connectionString instead.", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
    public class MsmqMessageQueueConfig : ConfigurationSection
    {

        [ConfigurationProperty("UseDeadLetterQueue", IsRequired = false, DefaultValue = true)]
        public bool UseDeadLetterQueue
        {
            get
            {
                return (bool)this["UseDeadLetterQueue"];
            }
            set
            {
                this["UseDeadLetterQueue"] = value;
            }
        }

        [ConfigurationProperty("UseJournalQueue", IsRequired = false)]
        public bool UseJournalQueue
        {
            get
            {
                return (bool)this["UseJournalQueue"];
            }
            set
            {
                this["UseJournalQueue"] = value;
            }
        }
    }
}
