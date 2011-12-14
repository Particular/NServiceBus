namespace NServiceBus.Testing.Tests
{
    using System;
    using NUnit.Framework;
    using Saga;

    [TestFixture]
    public class Timeouts
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            Test.Initialize(typeof(MyTimeout));
        }

        [Test]
        public void Should_assert_30_style_timeouts_being_set()
        {
            Test.Saga<TimeoutSaga>()
                .ExpectTimeoutToBeSet<MyTimeout>()
                .When(saga => saga.Handle(new StartMessage()));
        }

        [Test]
        public void Should_assert_30_style_timeouts_being_set_together_with_other_timeouts()
        {
            Test.Saga<TimeoutSaga>()
                .ExpectTimeoutToBeSet<MyTimeout>()
                .When(saga => saga.Handle(new StartMessage()));
        }

        [Test]
        public void Should_assert_30_style_timeouts_being_set_with_the_correct_timespan()
        {
            Test.Saga<TimeoutSaga>()
                .ExpectTimeoutToBeSet<MyTimeout>(expiresIn => expiresIn == TimeSpan.FromDays(1))
                .When(saga => saga.Handle(new StartMessage()));
        }

        [Test]
        public void Should_assert_30_style_timeouts_being_set_with_the_correct_state()
        {
            Test.Saga<TimeoutSaga>()
                .ExpectTimeoutToBeSetWithState<MyTimeout>(state => state.SomeProperty == "Test")
                .When(saga => saga.Handle(new StartMessage()));
        }
    }

    class TimeoutSaga : Saga<TimeoutSagaData>,IHandleTimeouts<MyTimeout>,IAmStartedByMessages<StartMessage>
    {

        public void Handle(StartMessage message)
        {
            RequestUtcTimeout(TimeSpan.FromDays(1),new MyTimeout
                                                       {
                                                           SomeProperty = "Test"
                                                       });
            RequestUtcTimeout(TimeSpan.FromDays(1), new MyOtherTimeout());
        }
        public void Timeout(MyTimeout state)
        {
            Bus.Send(new SomeMessage());
        }

    }

    internal class StartMessage
    {
    }

    internal class SomeMessage : IMessage
    {
    }

    internal class MyTimeout:IMessage
    {
        public string SomeProperty { get; set; }
    }

    internal class MyOtherTimeout : IMessage
    {
    }

    class TimeoutSagaData : ISagaEntity
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }
}