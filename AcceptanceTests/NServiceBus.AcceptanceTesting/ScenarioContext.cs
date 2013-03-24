namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Runtime.Remoting.Activation;
    using System.Runtime.Remoting.Contexts;
    using System.Runtime.Remoting.Messaging;

    [Intercept]
    [Serializable]
    public abstract class ScenarioContext : ContextBoundObject
    {
        public event EventHandler ContextPropertyChanged;
        private TimeSpan testExecutionTimeout = new TimeSpan(0, 1, 30);

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
                        //Debug.WriteLine("Change: " + method);

                        Target.ContextPropertyChanged(Target, new EventArgs { });
                    }
                }

                return NextSink.SyncProcessMessage(msg);
            }
        }

        public bool EndpointsStarted { get; set; }

        public TimeSpan TestExecutionTimeout
        {
            get { return testExecutionTimeout; }
            set { testExecutionTimeout = value; }
        }
    }
}