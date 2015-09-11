namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Unicast.Behaviors;
    using NUnit.Framework;

    [TestFixture]
    [Explicit("Performance Tests")]
    public class HandlerInvocationCachePerformanceTests
    {
        [Test]
        public async Task RunNew()
        {
            var cache = new MessageHandlerRegistry(new Conventions());
            cache.RegisterHandler(typeof(StubMessageHandler));
            cache.RegisterHandler(typeof(StubTimeoutHandler));
            var stubMessage = new StubMessage();
            var stubTimeout = new StubTimeoutState();
            var messageHandler = cache.GetCachedHandlerForMessage<StubMessage>();
            var timeoutHandler = cache.GetCachedHandlerForMessage<StubTimeoutState>();

            await messageHandler.Invoke(stubMessage);
            await timeoutHandler.Invoke(stubTimeout);

            var startNew = Stopwatch.StartNew();
            for (var i = 0; i < 100000; i++)
            {
                await messageHandler.Invoke(stubMessage);
                await timeoutHandler.Invoke(stubTimeout);
            }
            startNew.Stop();
            Trace.WriteLine(startNew.ElapsedMilliseconds);
        }

        public class StubMessageHandler : IHandleMessages<StubMessage>
        {
            public void Handle(StubMessage message)
            {
            }
        }

        public class StubMessage : IMessage
        {
        }

        public class StubTimeoutHandler : IHandleTimeouts<StubTimeoutState>
        {
            public void Timeout(StubTimeoutState state)
            {
            }
        }

        public class StubTimeoutState : IMessage
        {
        }
    }

    [TestFixture]
    public class When_invoking_a_cached_message_handler
    {
        [Test]
        public async Task Should_invoke_handle_method()
        {
            var cache = new MessageHandlerRegistry(new Conventions());
            cache.RegisterHandler(typeof(StubHandler));

            var handler = cache.GetCachedHandlerForMessage<StubMessage>();
            await handler.Invoke(new StubMessage());

            Assert.IsTrue(((StubHandler) handler.Instance).HandleCalled);
        }

        [Test]
        public async Task Should_have_passed_through_correct_message()
        {
            var cache = new MessageHandlerRegistry(new Conventions());
            cache.RegisterHandler(typeof(StubHandler));

            var handler = cache.GetCachedHandlerForMessage<StubMessage>();
            var stubMessage = new StubMessage();
            await handler.Invoke(stubMessage);

            Assert.AreEqual(stubMessage, ((StubHandler) handler.Instance).HandledMessage);
        }

        public class StubHandler : IHandleMessages<StubMessage>
        {
            public void Handle(StubMessage message)
            {
                HandleCalled = true;
                HandledMessage = message;
            }

            public bool HandleCalled;
            public StubMessage HandledMessage;
        }

        public class StubMessage : IMessage
        {
        }
    }

    [TestFixture]
    public class When_invoking_a_cached_timeout_handler
    {
        [Test]
        public async void Should_invoke_timeout_method()
        {
            var cache = new MessageHandlerRegistry(new Conventions());
            cache.RegisterHandler(typeof(StubHandler));

            var handler = cache.GetCachedHandlerForMessage<StubTimeoutState>();
            await handler.Invoke(new StubTimeoutState());

            Assert.IsTrue(((StubHandler) handler.Instance).TimeoutCalled);
        }

        [Test]
        public async void Should_have_passed_through_correct_state()
        {
            var cache = new MessageHandlerRegistry(new Conventions());
            cache.RegisterHandler(typeof(StubHandler));

            var stubState = new StubTimeoutState();
            var handler = cache.GetCachedHandlerForMessage<StubTimeoutState>();
            await handler.Invoke(stubState);

            Assert.AreEqual(stubState, ((StubHandler) handler.Instance).HandledState);
        }

        public class StubHandler : IHandleTimeouts<StubTimeoutState>
        {
            public void Timeout(StubTimeoutState state)
            {
                TimeoutCalled = true;
                HandledState = state;
            }

            public StubTimeoutState HandledState;
            public bool TimeoutCalled;
        }

        public class StubTimeoutState : IMessage
        {
        }
    }

    static class MessageHandlerRegistryExtension
    {
        public static MessageHandler GetCachedHandlerForMessage<TMessage>(this MessageHandlerRegistry cache)
        {
            var handler = cache.GetHandlersFor(typeof(TMessage)).Single();
            handler.Instance = Activator.CreateInstance(handler.HandlerType);
            return handler;
        }
    }
}