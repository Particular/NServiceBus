using NUnit.Framework;

namespace NServiceBus.Testing.Tests
{
    [TestFixture]
    public class TestHandlerFixture
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            Test.Initialize(typeof(IMessage).Assembly, typeof(TestHandlerFixture).Assembly);
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
        
        [Test]
		public void ShouldCallHandleOnExplicitInterfaceImplementation()
		{
			var handler = new ExplicitInterfaceImplementation();
			Assert.IsFalse(handler.IsHandled);
			Test.Handler(handler).OnMessage<TestMessage>(m => { });
			Assert.IsTrue(handler.IsHandled);
		}

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
        
		public class ExplicitInterfaceImplementation : IHandleMessages<TestMessage>
		{

			public bool IsHandled { get; set; }

			void IMessageHandler<TestMessage>.Handle(TestMessage message) {
				IsHandled = true;
			}

			// Unit test fails if this is uncommented; seems to me that this should
			// be made to pass, but it looks like a design decision based on commit
			// revision 1210.
			//public void Handle(TestMessage message) {
			//    throw new System.Exception("Shouldn't call this.");
			//}

		}

    }

    public interface TestMessage : IMessage { }
}