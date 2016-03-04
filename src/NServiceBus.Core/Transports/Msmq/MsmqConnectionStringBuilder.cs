namespace NServiceBus
{
    using System;
    using System.Data.Common;

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
            {
                settings.UseDeadLetterQueue = bool.Parse((string) this["deadLetter"]);
            }

            if (ContainsKey("journal"))
            {
                settings.UseJournalQueue = bool.Parse((string) this["journal"]);
            }

            if (ContainsKey("cacheSendConnection"))
            {
                settings.UseConnectionCache = bool.Parse((string) this["cacheSendConnection"]);
            }

            if (ContainsKey("useTransactionalQueues"))
            {
                settings.UseTransactionalQueues = bool.Parse((string) this["useTransactionalQueues"]);
            }

            if (ContainsKey("timeToReachQueue"))
            {
                settings.TimeToReachQueue = TimeSpan.Parse((string) this["timeToReachQueue"]);
            }

            return settings;
        }
    }
}