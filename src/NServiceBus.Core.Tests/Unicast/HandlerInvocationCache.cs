namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Unicast.Behaviors;
    using NUnit.Framework;
    using PublishOptions = NServiceBus.PublishOptions;
    using ReplyOptions = NServiceBus.ReplyOptions;
    using SendOptions = NServiceBus.SendOptions;

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

            await messageHandler.Invoke(stubMessage, null);
            await timeoutHandler.Invoke(stubTimeout, null);

            var startNew = Stopwatch.StartNew();
            for (var i = 0; i < 100000; i++)
            {
                await messageHandler.Invoke(stubMessage, null);
                await timeoutHandler.Invoke(stubTimeout, null);
            }
            startNew.Stop();
            Trace.WriteLine(startNew.ElapsedMilliseconds);
        }

        public class StubMessageHandler : IHandleMessages<StubMessage>
        {
            public Task Handle(StubMessage message, IMessageHandlerContext context)
            {
                return Task.FromResult(0);
            }
        }

        public class StubMessage : IMessage
        {
        }

        public class StubTimeoutHandler : IHandleTimeouts<StubTimeoutState>
        {
            public Task Timeout(StubTimeoutState state, IMessageHandlerContext context)
            {
                return TaskEx.Completed;
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
            await handler.Invoke(new StubMessage(), null);

            Assert.IsTrue(((StubHandler)handler.Instance).HandleCalled);
        }

        [Test]
        public async Task Should_have_passed_through_correct_message()
        {
            var cache = new MessageHandlerRegistry(new Conventions());
            cache.RegisterHandler(typeof(StubHandler));

            var handler = cache.GetCachedHandlerForMessage<StubMessage>();
            var stubMessage = new StubMessage();
            await handler.Invoke(stubMessage, null);

            Assert.AreEqual(stubMessage, ((StubHandler)handler.Instance).HandledMessage);
        }

        [Test]
        public async Task Should_have_passed_through_correct_context()
        {
            var cache = new MessageHandlerRegistry(new Conventions());
            cache.RegisterHandler(typeof(StubHandler));

            var handler = cache.GetCachedHandlerForMessage<StubMessage>();
            var handlerContext = new FakeMessageHandlerContext();
            await handler.Invoke(new StubMessage(), handlerContext);

            Assert.AreSame(handlerContext, ((StubHandler)handler.Instance).HandlerContext);
        }

        public class StubHandler : IHandleMessages<StubMessage>
        {
            public Task Handle(StubMessage message, IMessageHandlerContext context)
            {
                HandleCalled = true;
                HandledMessage = message;
                HandlerContext = context;
                return TaskEx.Completed;
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
        public async void Should_invoke_timeout_method()
        {
            var cache = new MessageHandlerRegistry(new Conventions());
            cache.RegisterHandler(typeof(StubHandler));

            var handler = cache.GetCachedHandlerForMessage<StubTimeoutState>();
            await handler.Invoke(new StubTimeoutState(), null);

            Assert.IsTrue(((StubHandler)handler.Instance).TimeoutCalled);
        }

        [Test]
        public async void Should_have_passed_through_correct_state()
        {
            var cache = new MessageHandlerRegistry(new Conventions());
            cache.RegisterHandler(typeof(StubHandler));

            var stubState = new StubTimeoutState();
            var handler = cache.GetCachedHandlerForMessage<StubTimeoutState>();
            await handler.Invoke(stubState, null);

            Assert.AreEqual(stubState, ((StubHandler)handler.Instance).HandledState);
        }

        [Test]
        public async Task Should_have_passed_through_correct_context()
        {
            var cache = new MessageHandlerRegistry(new Conventions());
            cache.RegisterHandler(typeof(StubHandler));

            var handler = cache.GetCachedHandlerForMessage<StubTimeoutState>();
            var handlerContext = new FakeMessageHandlerContext();
            await handler.Invoke(new StubTimeoutState(), handlerContext);

            Assert.AreSame(handlerContext, ((StubHandler)handler.Instance).HandlerContext);
        }

        public class StubHandler : IHandleTimeouts<StubTimeoutState>
        {
            public Task Timeout(StubTimeoutState state, IMessageHandlerContext context)
            {
                TimeoutCalled = true;
                HandledState = state;
                HandlerContext = context;
                return TaskEx.Completed;
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

    class FakeMessageHandlerContext : IMessageHandlerContext
    {
        public string MessageId { get; }
        public string ReplyToAddress { get; }
        public IReadOnlyDictionary<string, string> MessageHeaders { get; }
        public ContextBag Extensions { get; }
        public Task Send(object message, SendOptions options)
        {
            throw new NotImplementedException();
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            throw new NotImplementedException();
        }

        public Task Publish(object message, PublishOptions options)
        {
            throw new NotImplementedException();
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            throw new NotImplementedException();
        }

        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            throw new NotImplementedException();
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            throw new NotImplementedException();
        }

        public Task Reply(object message, ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        public Task Reply<T>(Action<T> messageConstructor, ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        public Task HandleCurrentMessageLater()
        {
            throw new NotImplementedException();
        }

        public Task ForwardCurrentMessageTo(string destination)
        {
            throw new NotImplementedException();
        }

        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            throw new NotImplementedException();
        }
    }
}