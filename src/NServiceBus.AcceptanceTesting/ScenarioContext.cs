namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using Faults;
    using Logging;

    public class ScenarioContext
    {
        internal static ScenarioContext Current
        {
            get => asyncContext.Value;
            set => asyncContext.Value = value;
        }

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

        public LogLevel LogLevel { get; set; } = LogLevel.Debug;

        internal ConcurrentDictionary<string, bool> UnfinishedFailedMessages = new ConcurrentDictionary<string, bool>();

        static readonly AsyncLocal<ScenarioContext> asyncContext = new AsyncLocal<ScenarioContext>();

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