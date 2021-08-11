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

        internal static string CurrentEndpoint
        {
            get => asyncEndpointName.Value;
            set => asyncEndpointName.Value = value;
        }

        public Guid TestRunId { get; } = Guid.NewGuid();

        public bool EndpointsStarted { get; set; }

        public bool HasNativePubSubSupport { get; set; }

        public void AddTrace(string trace)
        {
            Logs.Enqueue(new LogItem
            {
                LoggerName = "Trace",
                Level = LogLevel.Info,
                Message = trace
            });
        }

        public ConcurrentDictionary<string, IReadOnlyCollection<FailedMessage>> FailedMessages = new ConcurrentDictionary<string, IReadOnlyCollection<FailedMessage>>();

        public ConcurrentQueue<LogItem> Logs = new ConcurrentQueue<LogItem>();

        public LogLevel LogLevel { get; set; } = LogLevel.Debug;

        internal ConcurrentDictionary<string, bool> UnfinishedFailedMessages = new ConcurrentDictionary<string, bool>();

        static readonly AsyncLocal<ScenarioContext> asyncContext = new AsyncLocal<ScenarioContext>();
        static readonly AsyncLocal<string> asyncEndpointName = new AsyncLocal<string>();

        public class LogItem
        {
#pragma warning disable PS0023 // DateTime.UtcNow or DateTimeOffset.UtcNow should be used instead of DateTime.Now and DateTimeOffset.Now, unless the value is being used for displaying the current date-time in a user's local time zone
            public DateTime Timestamp { get; } = DateTime.Now;
#pragma warning restore PS0023 // DateTime.UtcNow or DateTimeOffset.UtcNow should be used instead of DateTime.Now and DateTimeOffset.Now, unless the value is being used for displaying the current date-time in a user's local time zone
            public string Endpoint { get; set; }
            public string LoggerName { get; set; }
            public string Message { get; set; }
            public LogLevel Level { get; set; }
        }
    }
}