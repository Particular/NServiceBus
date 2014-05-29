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
            Test.Initialize();
        }

        [Test]
        public void Should_assert_30_style_timeouts_being_set()
        {
            Test.Saga<TimeoutSaga>()
                .ExpectTimeoutToBeSetIn<MyTimeout>()
                .When(saga => saga.Handle(new StartMessage()));
        }

        [Test]
        public void Should_assert_30_style_timeouts_being_set_together_with_other_timeouts()
        {
            Test.Saga<TimeoutSaga>()
                .ExpectTimeoutToBeSetIn<MyTimeout>()
                .When(saga => saga.Handle(new StartMessage()));
        }

        [Test]
        public void Should_assert_30_style_timeouts_being_set_with_the_correct_timeSpan()
        {
            Test.Saga<TimeoutSaga>()
                .ExpectTimeoutToBeSetIn<MyTimeout>((state, expiresIn) => expiresIn == TimeSpan.FromDays(1))
                .When(saga => saga.Handle(new StartMessage()));
        }

        [Test]
        public void Should_assert_30_style_timeouts_being_set_with_the_correct_state()
        {
            Test.Saga<TimeoutSaga>()
                .ExpectTimeoutToBeSetIn<MyTimeout>((state, expiresIn) => state.SomeProperty == "Test")
                .When(saga => saga.Handle(new StartMessage()));
        }
    }

    class TimeoutSaga : Saga<TimeoutSagaData>,
                                 IHandleTimeouts<MyTimeout>,
                                 IHandleTimeouts<MyOtherTimeout>,
                                 IAmStartedByMessages<StartMessage>
    {

        public void Handle(StartMessage message)
        {
            RequestTimeout(TimeSpan.FromDays(1), new MyTimeout
                                                        {
                                                            SomeProperty = "Test"
                                                        });
            RequestTimeout<MyOtherTimeout>(TimeSpan.FromDays(1));
        }

        public void Timeout(MyTimeout state)
        {
            Bus.Send(new SomeMessage());
        }

        public void Timeout(MyOtherTimeout state)
        {
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TimeoutSagaData> mapper)
        {
        }
    }

    class StartMessage
    {
    }

    class SomeMessage : IMessage
    {
    }

    class MyTimeout
    {
        public string SomeProperty { get; set; }
    }

    class MyOtherTimeout
    {
    }

    class TimeoutSagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }
}
