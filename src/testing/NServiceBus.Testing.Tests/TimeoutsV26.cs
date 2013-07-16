namespace NServiceBus.Testing.Tests
{
    using System;
    using System.Reflection;
    using NServiceBus;
    using NUnit.Framework;
    using Saga;
    using Testing;


    [TestFixture]
    public class TimeoutsV26
    {
        [TestFixtureSetUp]
        public void Init()
        {
            Test.Initialize(Assembly.GetExecutingAssembly(), typeof(CompletionResult).Assembly);
        }

        [Test]
        public void WhenMessageATimeout()
        {
            Test.Saga<SagaTest>()
                .ExpectTimeoutToBeSetIn<int>((flag, span) => flag == 1 && span.TotalDays == 30)
                .When(saga => saga.Handle(new SagaTestEvent()))
                .AssertSagaCompletionIs(false);
        }
    }

    public class SagaTest : Saga.Saga<Data>, IAmStartedByMessages<SagaTestEvent>
    {
#pragma warning disable 0672
        public override void Timeout(object state)
        {
            if ((int)state == 1)
                MarkAsComplete();
        }
#pragma warning restore 0672

#pragma warning disable 0618
        public void Handle(SagaTestEvent message)
        {
            RequestUtcTimeout(TimeSpan.FromDays(30), 1);
        }
#pragma warning restore 0618
    }

    public class SagaTestEvent : IEvent {}
    public class Data : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }
}
