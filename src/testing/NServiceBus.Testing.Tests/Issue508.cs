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
            var expected = DateTime.UtcNow.AddDays(-3);
            var message = new TheMessage { TimeoutAt = expected };

            Test
                .Saga<TheSaga>()
                .ExpectTimeoutToBeSetAt<TheTimeout>((m, at) => at == expected)
                .When(s => s.Handle(message));
        }

        [Test]
        public void TimeoutInThePastWithSendOnTimeout()
        {
            var message = new TheMessage { TimeoutAt = DateTime.UtcNow.AddDays(-3) };

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
            var expected = DateTime.UtcNow.AddDays(3);
            var message = new TheMessage { TimeoutAt = expected };

            Test
                .Saga<TheSaga>()
                .ExpectTimeoutToBeSetAt<TheTimeout>((m, at) => at == expected)
                .When(s => s.Handle(message));
        }
    }

    public class TheSaga : Saga<TheData>,
                           IAmStartedByMessages<TheMessage>,
                           IHandleTimeouts<TheTimeout>
    {
        public void Handle(TheMessage message)
        {
            RequestUtcTimeout<TheTimeout>(message.TimeoutAt);
        }

        public void Timeout(TheTimeout state)
        {
            Bus.Send(new TheMessageSentAtTimeout());
            MarkAsComplete();
        }
    }

    public class TheData : IContainSagaData
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