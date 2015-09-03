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
            Trace += string.Format("{0:HH:mm:ss.ffffff} - {1}{2}", DateTime.Now, trace, Environment.NewLine);
        }

        public ConcurrentQueue<Exception> Exceptions = new ConcurrentQueue<Exception>();

        public ConcurrentQueue<LogItem> Logs = new ConcurrentQueue<LogItem>();

        public class LogItem
        {
            public string Message { get; set; }
            public string Level { get; set; }

            public override string ToString()
            {
                return string.Format("{0}: {1}", Level, Message);
            }
        }
    }
}