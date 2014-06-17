namespace NServiceBus
{
    using System;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "config.UsePersistence<Msmq>()")]
    public static class ConfigureMsmqSubscriptionStorage
    {
        /// <summary>
        /// Stores subscription data using MSMQ.
        /// If multiple machines need to share the same list of subscribers,
        /// you should not choose this option - prefer the DbSubscriptionStorage
        /// in that case.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "6",TreatAsErrorFromVersion = "5",Replacement = "config.UsePersistence<Msmq>()")]
// ReSharper disable once UnusedParameter.Global
        public static Configure MsmqSubscriptionStorage(this Configure config)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Stores subscription data using MSMQ.
        /// If multiple machines need to share the same list of subscribers,
        /// you should not choose this option - prefer the DbSubscriptionStorage
        /// in that case.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "config.UsePersistence<Msmq>(c=>c.QueueName('SomeName'))")]
// ReSharper disable UnusedParameter.Global
        public static Configure MsmqSubscriptionStorage(this Configure config, string endpointName)
// ReSharper restore UnusedParameter.Global
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Queue used to store subscriptions.
        /// </summary>
         [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "config.UsePersistence<Msmq>(c=>c.QueueName('SomeName'))")]
        public static Address Queue { get; set; }
    }
}
