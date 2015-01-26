namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Remoting.Activation;
    using System.Runtime.Remoting.Contexts;
    using System.Runtime.Remoting.Messaging;

    [Intercept]
    [Serializable]
    public abstract class ScenarioContext : ContextBoundObject
    {
        public event EventHandler ContextPropertyChanged;

        [AttributeUsage(AttributeTargets.Class)]
        sealed class InterceptAttribute : ContextAttribute, IContributeObjectSink
        {
            public InterceptAttribute()
                : base("InterceptProperty")
            {
            }

            public override void GetPropertiesForNewContext(IConstructionCallMessage message)
            {
                message.ContextProperties.Add(this);
            }

            public IMessageSink GetObjectSink(MarshalByRefObject obj, IMessageSink nextSink)
            {
                return new InterceptSink { Target = (ScenarioContext)obj, NextSink = nextSink };
            }
        }

        class InterceptSink : IMessageSink
        {
            public IMessageSink NextSink { get; set; }

            public ScenarioContext Target;

            public IMessageCtrl AsyncProcessMessage(IMessage msg, IMessageSink sink)
            {
                throw new NotSupportedException("AsyncProcessMessage is not supported.");
            }

            public IMessage SyncProcessMessage(IMessage msg)
            {
                var call = msg as IMethodCallMessage;
                if (call != null)
                {
                    var method = call.MethodName;


                    if (Target.ContextPropertyChanged != null && method.StartsWith("set"))
                    {
                        Target.ContextPropertyChanged(Target, EventArgs.Empty);
                    }
                }

                return NextSink.SyncProcessMessage(msg);
            }
        }

        public bool EndpointsStarted { get; set; }
        public string Exceptions { get; set; }

        public bool HasNativePubSubSupport { get; set; }

        public string Trace { get; set; }

        public void AddTrace(string trace)
        {
            Trace += DateTime.Now.ToString("HH:mm:ss.ffffff") + " - " + trace + Environment.NewLine;
        }

        public void RecordEndpointLog(string endpointName,string level ,string message)
        {
            endpointLogs.Add(new EndpointLogItem
            {
                Endpoint = endpointName,
                Level = level,
                Message = message
            });
        }


        public List<EndpointLogItem> GetAllLogs()
        {
            return endpointLogs.ToList();
        }


        List<EndpointLogItem> endpointLogs = new List<EndpointLogItem>();

        public class EndpointLogItem
        {
            public string Endpoint { get; set; }
            public string Message { get; set; }
            public string Level { get; set; }

            public override string ToString()
            {
                return string.Format("{0}({1}) - {2}", Level, Endpoint, Message);
            }
        }
    }
}