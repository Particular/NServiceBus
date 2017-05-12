namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Faults;
    using Logging;

    public class ScenarioContext
    {
        public Guid TestRunId { get; } = Guid.NewGuid();

        public bool EndpointsStarted { get; set; }

        public bool HasNativePubSubSupport { get; set; }

        public void AddTrace(string trace)
        {
            Logs.Enqueue(new LogItem
            {
                Level = LogLevel.Info,
                Message = trace
            });
        }

        public ConcurrentDictionary<string, IReadOnlyCollection<FailedMessage>> FailedMessages = new ConcurrentDictionary<string, IReadOnlyCollection<FailedMessage>>();

        public ConcurrentQueue<LogItem> Logs = new ConcurrentQueue<LogItem>();

        internal LogLevel LogLevel { get; set; } = LogLevel.Debug;

        internal ConcurrentDictionary<string, bool> UnfinishedFailedMessages = new ConcurrentDictionary<string, bool>();

        public void SetLogLevel(LogLevel level)
        {
            LogLevel = level;
        }

        public class LogItem
        {
            public string Message { get; set; }
            public LogLevel Level { get; set; }

            public override string ToString()
            {
                return $"{Level}: {Message}";
            }
        }
    }
}