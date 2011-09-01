using System;
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
            ExtensionMethods.IsMessageTypeAction = t => typeof (IMessage).IsAssignableFrom(t) && t != typeof (IMessage);
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
		[Test]
		public void ShouldPassExpectPublishWhenPublishing()
		{
			Test.Handler<PublishingHandler<Publish1>>()
				.ExpectPublish<Publish1>(m => true)
				.OnMessage<TestMessage>(m => { });
		}

		[Test]
		[ExpectedException]
		public void ShouldFailExpectNotPublishWhenPublishing()
		{
			Test.Handler<PublishingHandler<Publish1>>()
				.ExpectNotPublish<Publish1>(m => true)
				.OnMessage<TestMessage>(m => { });
		}

		[Test]
		public void ShouldPassExpectPublishWhenPublishingMultipleEvents()
		{
			Test.Handler<PublishingHandler<Publish1, Publish2>>()
				.ExpectPublish<Publish1>(m => true)
				.OnMessage<TestMessage>(m => { });
		}

        [Test]
        public void ShouldPassExpectPublishWhenMessageIsSend()
        {
            Test.Handler<PublishingHandler<Publish1>>()
                .ExpectPublish<Publish1>(m => true)
                .OnMessage(new TestMessageImpl(), Guid.NewGuid().ToString());
        }

        private class TestMessageImpl : TestMessage{}
        

		[Test]
		public void ShouldPassExpectPublishWhenPublishingAndCheckingPredicate()
		{
			Test.Handler<PublishingHandler<Publish1>>()
				.WithExternalDependencies(h => h.ModifyPublish = m => m.Data = "Data")
				.ExpectPublish<Publish1>(m => m.Data == "Data")
				.OnMessage<TestMessage>(m => { });
		}

		[Test]
		[ExpectedException]
		public void ShouldFailExpectNotPublishWhenPublishingAndCheckingPredicate()
		{
			Test.Handler<PublishingHandler<Publish1>>()
				.WithExternalDependencies(h => h.ModifyPublish = m => m.Data = "Data")
				.ExpectNotPublish<Publish1>(m => m.Data == "Data")
				.OnMessage<TestMessage>(m => { });
		}

		[Test]
		[ExpectedException]
		public void ShouldFailExpectPublishWhenPublishingAndCheckingPredicateThatFails()
		{
			Test.Handler<PublishingHandler<Publish1>>()
				.WithExternalDependencies(h => h.ModifyPublish = m => m.Data = "NotData")
				.ExpectPublish<Publish1>(m => m.Data == "Data")
				.OnMessage<TestMessage>(m => { });
		}

		[Test]
		public void ShouldPassExpectNotPublishWhenPublishingAndCheckingPredicateThatFails()
		{
			Test.Handler<PublishingHandler<Publish1>>()
				.WithExternalDependencies(h => h.ModifyPublish = m => m.Data = "NotData")
				.ExpectNotPublish<Publish1>(m => m.Data == "Data")
				.OnMessage<TestMessage>(m => { });
		}

		[Test]
		[ExpectedException]
		public void ShouldFailExpectPublishIfNotPublishing()
		{
			Test.Handler<EmptyHandler>()
				.ExpectPublish<Publish1>(m => true)
				.OnMessage<TestMessage>(m => { });
		}

		[Test]
		public void ShouldPassExpectNotPublishIfNotPublishing()
		{
			Test.Handler<EmptyHandler>()
				.ExpectNotPublish<Publish1>(m => true)
				.OnMessage<TestMessage>(m => { });
		}

		[Test]
		[ExpectedException]
		public void ShouldFailExpectPublishIfPublishWrongMessageType()
		{
			Test.Handler<PublishingHandler<Publish1>>()
				.ExpectPublish<Publish2>(m => true)
				.OnMessage<TestMessage>(m => { });
		}

		[Test]
		public void ShouldPassExpectNotPublishIfPublishWrongMessageType()
		{
			Test.Handler<PublishingHandler<Publish1>>()
				.ExpectNotPublish<Publish2>(m => true)
				.OnMessage<TestMessage>(m => { });
		}

        public class EmptyHandler : IHandleMessages<TestMessage>
        {
            public void Handle(TestMessage message) {}
        }

		public interface Publish1 : IMessage
		{
			string Data { get; set; }
		}
		public interface Publish2 : IMessage
		{
			string Data { get; set; }
		}
		public class PublishingHandler<TPublish> : IHandleMessages<TestMessage>
			where TPublish : IMessage
		{
			public IBus Bus { get; set; }
			public Action<TPublish> ModifyPublish { get; set; }

			public PublishingHandler()
			{
				ModifyPublish = m => { };
			}

			public void Handle(TestMessage message)
			{
				Bus.Publish(ModifyPublish);
			}
		}
		public class PublishingHandler<TPublish1, TPublish2> : IHandleMessages<TestMessage>
			where TPublish1 : IMessage
			where TPublish2 : IMessage
		{
			public IBus Bus { get; set; }
			public Action<TPublish1> ModifyPublish1 { get; set; }
			public Action<TPublish2> ModifyPublish2 { get; set; }

			public PublishingHandler()
			{
				ModifyPublish1 = m => { };
				ModifyPublish2 = m => { };
			}

			public void Handle(TestMessage message)
			{
				Bus.Publish(ModifyPublish1);
				Bus.Publish(ModifyPublish2);
			}
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