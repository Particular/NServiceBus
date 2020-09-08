namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    [Explicit("Performance Tests")]
    public class HandlerInvocationCachePerformanceTests
    {
        [Test]
        public async Task RunNew()
        {
            var cache = new MessageHandlerRegistry();
            cache.RegisterHandler(typeof(StubMessageHandler));
            cache.RegisterHandler(typeof(StubTimeoutHandler));
            var stubMessage = new StubMessage();
            var stubTimeout = new StubTimeoutState();
            var messageHandler = cache.GetCachedHandlerForMessage<StubMessage>();
            var timeoutHandler = cache.GetCachedHandlerForMessage<StubTimeoutState>();

            var fakeContext = new TestableMessageHandlerContext();

            await messageHandler.Invoke(stubMessage, fakeContext, CancellationToken.None);
            await timeoutHandler.Invoke(stubTimeout, fakeContext, CancellationToken.None);

            var startNew = Stopwatch.StartNew();
            for (var i = 0; i < 100000; i++)
            {
                await messageHandler.Invoke(stubMessage, fakeContext, CancellationToken.None);
                await timeoutHandler.Invoke(stubTimeout, fakeContext, CancellationToken.None);
            }
            startNew.Stop();
            Trace.WriteLine(startNew.ElapsedMilliseconds);
        }

        public class StubMessageHandler : IHandleMessages<StubMessage>
        {
            public Task Handle(StubMessage message, IMessageHandlerContext context)
            {
                return Task.CompletedTask;
            }
        }

        public class StubMessage : IMessage
        {
        }

        public class StubTimeoutHandler : IHandleTimeouts<StubTimeoutState>
        {
            public Task Timeout(StubTimeoutState state, IMessageHandlerContext context)
            {
                return Task.CompletedTask;
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
            var cache = new MessageHandlerRegistry();
            cache.RegisterHandler(typeof(StubHandler));

            var handler = cache.GetCachedHandlerForMessage<StubMessage>();
            var handlerContext = new TestableMessageHandlerContext();
            await handler.Invoke(new StubMessage(), handlerContext, CancellationToken.None);

            Assert.IsTrue(((StubHandler)handler.Instance).HandleCalled);
        }

        [Test]
        public async Task Should_have_passed_through_correct_message()
        {
            var cache = new MessageHandlerRegistry();
            cache.RegisterHandler(typeof(StubHandler));

            var handler = cache.GetCachedHandlerForMessage<StubMessage>();
            var stubMessage = new StubMessage();
            var handlerContext = new TestableMessageHandlerContext();
            await handler.Invoke(stubMessage, handlerContext, CancellationToken.None);

            Assert.AreEqual(stubMessage, ((StubHandler)handler.Instance).HandledMessage);
        }

        [Test]
        public async Task Should_have_passed_through_correct_context()
        {
            var cache = new MessageHandlerRegistry();
            cache.RegisterHandler(typeof(StubHandler));

            var handler = cache.GetCachedHandlerForMessage<StubMessage>();
            var handlerContext = new TestableMessageHandlerContext();
            await handler.Invoke(new StubMessage(), handlerContext, CancellationToken.None);

            Assert.AreSame(handlerContext, ((StubHandler)handler.Instance).HandlerContext);
        }

        public class StubHandler : IHandleMessages<StubMessage>
        {
            public Task Handle(StubMessage message, IMessageHandlerContext context)
            {
                HandleCalled = true;
                HandledMessage = message;
                HandlerContext = context;
                return Task.CompletedTask;
            }

            public bool HandleCalled;
            public StubMessage HandledMessage;
            public IMessageHandlerContext HandlerContext;
        }

        public class StubMessage : IMessage
        {
        }
    }

    [TestFixture]
    public class When_invoking_a_cached_timeout_handler
    {
        [Test]
        public async Task Should_invoke_timeout_method()
        {
            var cache = new MessageHandlerRegistry();
            cache.RegisterHandler(typeof(StubHandler));

            var handler = cache.GetCachedHandlerForMessage<StubTimeoutState>();
            var handlerContext = new TestableMessageHandlerContext();
            await handler.Invoke(new StubTimeoutState(), handlerContext, CancellationToken.None);

            Assert.IsTrue(((StubHandler)handler.Instance).TimeoutCalled);
        }

        [Test]
        public async Task Should_have_passed_through_correct_state()
        {
            var cache = new MessageHandlerRegistry();
            cache.RegisterHandler(typeof(StubHandler));

            var stubState = new StubTimeoutState();
            var handler = cache.GetCachedHandlerForMessage<StubTimeoutState>();
            var handlerContext = new TestableMessageHandlerContext();
            await handler.Invoke(stubState, handlerContext, CancellationToken.None);

            Assert.AreEqual(stubState, ((StubHandler)handler.Instance).HandledState);
        }

        [Test]
        public async Task Should_have_passed_through_correct_context()
        {
            var cache = new MessageHandlerRegistry();
            cache.RegisterHandler(typeof(StubHandler));

            var handler = cache.GetCachedHandlerForMessage<StubTimeoutState>();
            var handlerContext = new TestableMessageHandlerContext();
            await handler.Invoke(new StubTimeoutState(), handlerContext, CancellationToken.None);

            Assert.AreSame(handlerContext, ((StubHandler)handler.Instance).HandlerContext);
        }

        public class StubHandler : IHandleTimeouts<StubTimeoutState>
        {
            public Task Timeout(StubTimeoutState state, IMessageHandlerContext context)
            {
                TimeoutCalled = true;
                HandledState = state;
                HandlerContext = context;
                return Task.CompletedTask;
            }

            public StubTimeoutState HandledState;
            public bool TimeoutCalled;
            public IMessageHandlerContext HandlerContext;
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