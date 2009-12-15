using NUnit.Framework;

namespace NServiceBus.Testing.Tests
{
    [TestFixture]
    public class TestHandlerFixture
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            Test.Initialize();
        }

        [Test]
        public void ShouldAssertDoNotContinueDispatchingCurrentMessageToHandlersWasCalled()
        {
            Test.Handler<DoNotContinueDispatchingCurrentMessageToHandlersHandler>()
                .ExpectDoNotContinueDispatchingCurrentMessageToHandlers()
                .OnMessage<TestMessage>(m => {});
        }

        [Test]
        [ExpectedException]
        public void ShouldFailAssertingDoNotContinueDispatchingCurrentMessageToHandlersWasCalled()
        {
            Test.Handler<EmptyHandler>()
                .ExpectDoNotContinueDispatchingCurrentMessageToHandlers()
                .OnMessage<TestMessage>(m => {});
        }

        [Test]
        public void ShouldAssertHandleCurrentMessageLaterWasCalled()
        {
            Test.Handler<HandleCurrentMessageLaterHandler>()
                .ExpectHandleCurrentMessageLater()
                .OnMessage<TestMessage>(m => {});
        }

        [Test]
        [ExpectedException]
        public void ShouldFailAssertingHandleCurrentMessageLaterWasCalled()
        {
            Test.Handler<EmptyHandler>()
                .ExpectHandleCurrentMessageLater()
                .OnMessage<TestMessage>(m => {});
        }

        public interface TestMessage : IMessage {}

        public class EmptyHandler : IHandleMessages<TestMessage>
        {
            public void Handle(TestMessage message) {}
        }

        public class DoNotContinueDispatchingCurrentMessageToHandlersHandler : IHandleMessages<TestMessage>
        {
            public IBus Bus { get; set; }

            public void Handle(TestMessage message)
            {
                this.Bus.DoNotContinueDispatchingCurrentMessageToHandlers();
            }
        }

        public class HandleCurrentMessageLaterHandler : IHandleMessages<TestMessage>
        {
            public IBus Bus { get; set; }

            public void Handle(TestMessage message)
            {
                this.Bus.HandleCurrentMessageLater();
            }
        }
    }
}