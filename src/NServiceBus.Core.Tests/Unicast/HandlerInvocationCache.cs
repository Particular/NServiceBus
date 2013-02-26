using System.Diagnostics;
using NServiceBus.Saga;

namespace NServiceBus.Unicast.Tests
{
    using NUnit.Framework;

	[TestFixture]
	public class HandlerInvocationCachePerf
	{
		[Test]
		public void RunNew()
		{
			var type = typeof (StubHandler);
			HandlerInvocationCache.CacheMethodForHandler(type, typeof (StubMessage));
			var handler = new StubHandler();
			var stubMessage = new StubMessage();
			HandlerInvocationCache.InvokeHandle(handler, stubMessage);

			var startNew = Stopwatch.StartNew();
			for (var i = 0; i < 10000000; i++)
			{
				HandlerInvocationCache.InvokeHandle(handler, stubMessage);
			}
			Debug.WriteLine(startNew.ElapsedMilliseconds);
		}
		[Test]
		public void RunOld()
		{
			HandlerInvocationCacheOld.CacheMethodForHandler(typeof (StubHandler), typeof (StubMessage));
			var handler = new StubHandler();
			var stubMessage = new StubMessage();
			HandlerInvocationCacheOld.Invoke(typeof(IMessageHandler<>), handler, stubMessage);

			var startNew = Stopwatch.StartNew();
			for (var i = 0; i < 10000000; i++)
			{
				HandlerInvocationCacheOld.Invoke(typeof(IMessageHandler<>), handler, stubMessage);
			}
			Debug.WriteLine(startNew.ElapsedMilliseconds);
		}
		public class StubHandler : IMessageHandler<StubMessage>
		{

			public void Handle(StubMessage message)
			{
			}
		}

		public class StubMessage
		{
		}
	}

	[TestFixture]
	public class When_invoking_a_cached_message_handler
	{
		[Test]
		public void Should_invoke_handle_method()
		{
			HandlerInvocationCache.CacheMethodForHandler(typeof (StubHandler), typeof (StubMessage));
			var handler = new StubHandler();
			HandlerInvocationCache.InvokeHandle(handler, new StubMessage());
			Assert.IsTrue(handler.HandleCalled);
		}

		[Test]
		public void Should_have_passed_through_correct_message()
		{
			HandlerInvocationCache.CacheMethodForHandler(typeof (StubHandler), typeof (StubMessage));
			var handler = new StubHandler();
			var stubMessage = new StubMessage();
			HandlerInvocationCache.InvokeHandle(handler, stubMessage);
			Assert.AreEqual(stubMessage, handler.HandledMessage);
		}

		public class StubHandler : IMessageHandler<StubMessage>
		{
			public bool HandleCalled;
			public StubMessage HandledMessage;

			public void Handle(StubMessage message)
			{
				HandleCalled = true;
				HandledMessage = message;
			}
		}

		public class StubMessage
		{
		}

	}

	[TestFixture]
	public class When_invoking_a_cached_timout_handler
	{
		[Test]
		public void Should_invoke_timeout_method()
		{
			HandlerInvocationCache.CacheMethodForHandler(typeof(StubHandler), typeof(StubTimeoutState));
			var handler = new StubHandler();
			HandlerInvocationCache.InvokeTimeout(handler, new StubTimeoutState());
			Assert.IsTrue(handler.TimeoutCalled);
		}

		[Test]
		public void Should_have_passed_through_correct_state()
		{
			HandlerInvocationCache.CacheMethodForHandler(typeof(StubHandler), typeof(StubTimeoutState));
			var handler = new StubHandler();
			var stubState = new StubTimeoutState();
			HandlerInvocationCache.InvokeTimeout(handler, stubState);
			Assert.AreEqual(stubState, handler.HandledState);
		}

		public class StubHandler : IHandleTimeouts<StubTimeoutState>
		{
			public bool TimeoutCalled;
			public StubTimeoutState HandledState;


			public void Timeout(StubTimeoutState state)
			{
				TimeoutCalled = true;
				HandledState = state;
			}
		}

		public class StubTimeoutState
		{
		}

	}

}

