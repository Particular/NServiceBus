using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus.Saga;
using NUnit.Framework;

namespace NServiceBus.Testing.Tests
{
    [TestFixture]
    class SagaTests
    {
        [Test]
        public void A()
        {
            StartsSaga st = null;

            Test.Saga<MySaga>()
                    .ExpectReplyToOrginator<ResponseToOriginator>()
                    .ExpectTimeoutToBeSetIn<StartsSaga>((state, span) =>
                                                            {
                                                                st = state; 
                                                                return span == TimeSpan.FromDays(7); 
                                                            })
                    .ExpectPublish<Event>()
                    .ExpectSend<Command>()
                .When(s => s.Handle(new StartsSaga()))
                    .ExpectPublish<Event>()
                .When(s => s.Timeout(st))
                    .AssertSagaCompletionIs(true);
        }

        [TestFixtureSetUp]
        public void Setup()
        {
            Test.Initialize();
        }
    }

    public class MySaga : NServiceBus.Saga.Saga<MySagaData>,
        IAmStartedByMessages<StartsSaga>,
        IHandleTimeouts<StartsSaga>
    {
        public void Handle(StartsSaga message)
        {
            ReplyToOriginator<ResponseToOriginator>(m => { });
            Bus.Publish<Event>();
            Bus.Send<Command>(null);
            RequestUtcTimeout(TimeSpan.FromDays(7), message);
        }

        public void Timeout(StartsSaga state)
        {
            Bus.Publish<Event>();
            MarkAsComplete();
        }
    }

    public class MySagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }

    public class StartsSaga : ICommand, ITimeoutState {}
    public class ResponseToOriginator : IMessage {}
    public interface Event : IEvent { }
    public class Command : ICommand {}
}
