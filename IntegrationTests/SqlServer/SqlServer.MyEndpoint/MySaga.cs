namespace SqlServer.MyEndpoint
{
    using System;
    using NServiceBus;
    using NServiceBus.Saga;

    public class MySaga:Saga<MySaga.MySagaData>, 
        IAmStartedByMessages<StartSagaMessage>,
        IHandleTimeouts<MySaga.MyTimeout>
    {
        public void Handle(StartSagaMessage message)
        {
            Console.Out.WriteLine("Saga started, requesting timeout");
            RequestTimeout<MyTimeout>(TimeSpan.FromSeconds(10));
        }
        public void Timeout(MyTimeout state)
        {
            Console.Out.WriteLine("Timeout fired, completing saga");
            
            MarkAsComplete();
        }

        public class MySagaData : IContainSagaData
        {
            public virtual Guid Id { get; set; }
            public virtual string Originator { get; set; }
            public virtual string OriginalMessageId { get; set; }
        }

        public class MyTimeout
        {
        }


    }


    public class StartSagaMessage:IMessage
    {
    }
}