namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    class DelayedRetry : RecoveryAction
    {

        public DelayedRetry(TimeSpan delay, Dictionary<string, string> metadata)
        {
            Delay = delay;
            Metadata = metadata;
        }

        public TimeSpan Delay { get; }

        public Dictionary<string, string> Metadata { get; }
    }
}