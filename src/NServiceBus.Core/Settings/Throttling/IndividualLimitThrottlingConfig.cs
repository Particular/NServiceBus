namespace NServiceBus.Settings.Throttling
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;

    class IndividualLimitThrottlingConfig : IThrottlingConfig
    {
        readonly int? defaultMaximumMessagesPerSecond;
        readonly Dictionary<string, int?> throttlingOverrides;

        public IndividualLimitThrottlingConfig(int? defaultMaximumMessagesPerSecond, Dictionary<string, int?> throttlingOverrides)
        {
            this.defaultMaximumMessagesPerSecond = defaultMaximumMessagesPerSecond;
            this.throttlingOverrides = throttlingOverrides;
        }

        public IExecutor WrapExecutor(IExecutor rawExecutor)
        {
            return new IndividualThroughputLimitExecutor(defaultMaximumMessagesPerSecond, throttlingOverrides);
        }
    }
}