namespace NServiceBus.Settings.Throttling
{
    using NServiceBus.Pipeline;

    interface IThrottlingConfig
    {
        IExecutor WrapExecutor(IExecutor rawExecutor);
    }
}