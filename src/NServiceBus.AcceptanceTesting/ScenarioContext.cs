namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Concurrent;

    public abstract class ScenarioContext
    {
        public bool EndpointsStarted { get; set; }

        public bool HasNativePubSubSupport { get; set; }

        public string Trace { get; set; }

        public void AddTrace(string trace)
        {
            Trace += $"{DateTime.Now:HH:mm:ss.ffffff} - {trace}{Environment.NewLine}";
        }

        public ConcurrentQueue<Exception> Exceptions = new ConcurrentQueue<Exception>();

        public ConcurrentQueue<LogItem> Logs = new ConcurrentQueue<LogItem>();

        public class LogItem
        {
            public string Message { get; set; }
            public string Level { get; set; }

            public override string ToString()
            {
                return $"{Level}: {Message}";
            }
        }
    }
}