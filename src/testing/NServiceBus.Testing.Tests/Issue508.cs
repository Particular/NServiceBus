namespace NServiceBus.Testing.Tests
{
    using System;
    using MyMessages;
    using NUnit.Framework;
    using Saga;

    [TestFixture]
    public class Issue508
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            Test.Initialize();
        }

        [Test]
        public void TimeoutInThePast()
        {
            //This only works with DateTime.MinValue!
            var message = new TheMessage { TimeoutAt = DateTime.MinValue };

            Test
                .Saga<TheSaga>()
                .ExpectTimeoutToBeSetAt<TheTimeout>((m, at) => at == DateTime.MinValue.ToUniversalTime())
                .When(s => s.Handle(message));
        }

        [Test]
        public void TimeoutInThePastWithSendOnTimeout()
        {
            var message = new TheMessage { TimeoutAt = DateTime.MinValue };

            Test
                .Saga<TheSaga>()
                .ExpectTimeoutToBeSetAt<TheTimeout>((m, at) => true)
                .When(s => s.Handle(message))
                .ExpectSend<TheMessageSentAtTimeout>()
                .WhenSagaTimesOut();
        }

        [Test]
        public void TimeoutInTheFuture()
        {
            var message = new TheMessage { TimeoutAt = DateTime.MaxValue };

            Test
                .Saga<TheSaga>()
                .ExpectTimeoutToBeSetAt<TheTimeout>((m, at) => at == DateTime.MaxValue.ToUniversalTime())
                .When(s => s.Handle(message));
        }
    }

    public class TheSaga : Saga<TheData>,
                           IAmStartedByMessages<TheMessage>,
                           IHandleTimeouts<TheTimeout>
    {
        public void Handle(TheMessage message)
        {
            RequestUtcTimeout<TheTimeout>(message.TimeoutAt.ToUniversalTime());
        }

        public void Timeout(TheTimeout state)
        {
            Bus.Send(new TheMessageSentAtTimeout());
            MarkAsComplete();
        }
    }

    public class TheData : ISagaEntity
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }

    namespace MyMessages
    {
        using System;

        public class TheMessage : IMessage
        {
            public DateTime TimeoutAt { get; set; }
        }

        public class TheTimeout : IMessage
        {
        }

        public class TheMessageSentAtTimeout : IMessage
        {
        }
    }
}