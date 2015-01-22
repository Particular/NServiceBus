namespace NServiceBus.Settings.Concurrency
{
    using NServiceBus.Pipeline;

    interface IConcurrencyConfig
    {
        IExecutor BuildExecutor(BusNotifications busNotifications);
    }
}