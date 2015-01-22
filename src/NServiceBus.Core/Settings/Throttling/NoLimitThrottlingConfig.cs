namespace NServiceBus.Settings.Throttling
{
    using NServiceBus.Pipeline;

    class NoLimitThrottlingConfig : IThrottlingConfig
    {
        public IExecutor WrapExecutor(IExecutor rawExecutor)
        {
            return rawExecutor;
        }
    }
}