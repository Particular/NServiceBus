namespace Runner
{
    using NServiceBus.Config;
    using NServiceBus.Config.ConfigurationSource;

    class TransportConfigOverride : IProvideConfiguration<TransportConfig>
    {
        public static int MaximumConcurrencyLevel;
        public TransportConfig GetConfiguration()
        {
            return new TransportConfig
                {
                    MaximumConcurrencyLevel = MaximumConcurrencyLevel,
                    MaxRetries = 10,
                    MaximumMessageThroughputPerSecond = 0
                };
        }
    }


    class MsmqConfigOverride : IProvideConfiguration<MsmqMessageQueueConfig>
    {
        public MsmqMessageQueueConfig GetConfiguration()
        {
            return new MsmqMessageQueueConfig
            {
              UseDeadLetterQueue=false,
              UseJournalQueue = false
            };
        }
    }
}