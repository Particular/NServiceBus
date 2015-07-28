namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Generic;

    public abstract class ScenarioContext
    {
        public bool EndpointsStarted { get; set; }
        public string Exceptions { get; set; }

        public bool HasNativePubSubSupport { get; set; }

        public string Trace { get; set; }

        public void AddTrace(string trace)
        {
            Trace += String.Format("{0:HH:mm:ss.ffffff} - {1}{2}", DateTime.Now, trace, Environment.NewLine);
        }

        public void RecordEndpointLog(string level, string message)
        {
            Logs.Add(new LogItem
            {
                Level = level,
                Message = message
            });
        }

        public readonly List<LogItem> Logs = new List<LogItem>();

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