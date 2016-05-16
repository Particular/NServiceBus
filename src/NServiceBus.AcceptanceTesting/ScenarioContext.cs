namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Faults;
    using Logging;

    public abstract class ScenarioContext
    {
        public Guid TestRunId { get; } = Guid.NewGuid();

        public bool EndpointsStarted { get; set; }

        public bool HasNativePubSubSupport { get; set; }

        public string Trace => string.Join(Environment.NewLine, traceQueue.ToArray());

        public void AddTrace(string trace)
        {
            traceQueue.Enqueue($"{DateTime.Now:HH:mm:ss.ffffff} - {trace}");
        }

        public ConcurrentQueue<Exception> LoggedExceptions = new ConcurrentQueue<Exception>();

        public ConcurrentDictionary<string, IReadOnlyCollection<FailedMessage>> FailedMessages = new ConcurrentDictionary<string, IReadOnlyCollection<FailedMessage>>();

        public ConcurrentQueue<LogItem> Logs = new ConcurrentQueue<LogItem>();

        ConcurrentQueue<string> traceQueue = new ConcurrentQueue<string>();

        internal Dictionary<string, LogLevel> LogLevels = new Dictionary<string, LogLevel>();

        public void SetLogLevel(string logger, LogLevel level)
        {
            LogLevels[logger] = level;
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