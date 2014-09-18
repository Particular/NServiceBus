namespace NServiceBus.Config
{
    using System.Data.Common;
    using NServiceBus.Transports.Msmq.Config;

    class MsmqConnectionStringBuilder : DbConnectionStringBuilder
    {
        public MsmqConnectionStringBuilder(string connectionString)
        {
            
            ConnectionString = connectionString;
        }

        public MsmqSettings RetrieveSettings()
        {
            var settings = new MsmqSettings();

            if (ContainsKey("deadLetter"))
                settings.UseDeadLetterQueue = bool.Parse((string)this["deadLetter"]);

            if (ContainsKey("journal"))
                settings.UseJournalQueue = bool.Parse((string) this["journal"]);

            if (ContainsKey("cacheSendConnection"))
                settings.UseConnectionCache = bool.Parse((string)this["cacheSendConnection"]);

            if (ContainsKey("useTransactionalQueues"))
                settings.UseTransactionalQueues = bool.Parse((string)this["useTransactionalQueues"]);

           
            return settings;
        }
    }
}