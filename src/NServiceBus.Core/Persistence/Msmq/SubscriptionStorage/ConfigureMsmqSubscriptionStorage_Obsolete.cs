#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global

namespace NServiceBus
{
    using System;

    [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "config.UsePersistence<Msmq>()")]
    public static class ConfigureMsmqSubscriptionStorage
    {
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "config.UsePersistence<Msmq>()")]
        public static Configure MsmqSubscriptionStorage(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "config.UsePersistence<Msmq>(c=>c.QueueName('SomeName'))")]
        public static Configure MsmqSubscriptionStorage(this Configure config, string endpointName)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "config.UsePersistence<Msmq>(c=>c.QueueName('SomeName'))")]
        public static Address Queue { get; set; }
    }
}