namespace NServiceBus.Unicast.Tests;

using System;
using System.Diagnostics;
using System.Linq;
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
        cache.AddHandler<StubMessageHandler>();
        cache.AddHandler<StubTimeoutHandler>();
        var stubMessage = new StubMessage();
        var stubTimeout = new StubTimeoutState();
        var messageHandler = cache.GetCachedHandlerForMessage<StubMessage>();
        var timeoutHandler = cache.GetCachedHandlerForMessage<StubTimeoutState>();

        var fakeContext = new TestableMessageHandlerContext();

        await messageHandler.Invoke(stubMessage, fakeContext);
        await timeoutHandler.Invoke(stubTimeout, fakeContext);

        var startNew = Stopwatch.StartNew();
        for (var i = 0; i < 100000; i++)
        {
            await messageHandler.Invoke(stubMessage, fakeContext);
            await timeoutHandler.Invoke(stubTimeout, fakeContext);
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

    public class StubTimeoutHandler : IHandleTimeouts<StubTimeoutState>,
        IHandleMessages // Required to match generic constraint on AddHandler<THandler>()
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
        cache.AddHandler<StubHandler>();

        var handler = cache.GetCachedHandlerForMessage<StubMessage>();
        var handlerContext = new TestableMessageHandlerContext();
        await handler.Invoke(new StubMessage(), handlerContext);

        Assert.That(((StubHandler)handler.Instance).HandleCalled, Is.True);
    }

    [Test]
    public async Task Should_have_passed_through_correct_message()
    {
        var cache = new MessageHandlerRegistry();
        cache.AddHandler<StubHandler>();

        var handler = cache.GetCachedHandlerForMessage<StubMessage>();
        var stubMessage = new StubMessage();
        var handlerContext = new TestableMessageHandlerContext();
        await handler.Invoke(stubMessage, handlerContext);

        Assert.That(((StubHandler)handler.Instance).HandledMessage, Is.EqualTo(stubMessage));
    }

    [Test]
    public async Task Should_have_passed_through_correct_context()
    {
        var cache = new MessageHandlerRegistry();
        cache.AddHandler<StubHandler>();

        var handler = cache.GetCachedHandlerForMessage<StubMessage>();
        var handlerContext = new TestableMessageHandlerContext();
        await handler.Invoke(new StubMessage(), handlerContext);

        Assert.That(((StubHandler)handler.Instance).HandlerContext, Is.SameAs(handlerContext));
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
        cache.AddHandler<StubHandler>();

        var handler = cache.GetCachedHandlerForMessage<StubTimeoutState>();
        var handlerContext = new TestableMessageHandlerContext();
        await handler.Invoke(new StubTimeoutState(), handlerContext);

        Assert.That(((StubHandler)handler.Instance).TimeoutCalled, Is.True);
    }

    [Test]
    public async Task Should_have_passed_through_correct_state()
    {
        var cache = new MessageHandlerRegistry();
        cache.AddHandler<StubHandler>();

        var stubState = new StubTimeoutState();
        var handler = cache.GetCachedHandlerForMessage<StubTimeoutState>();
        var handlerContext = new TestableMessageHandlerContext();
        await handler.Invoke(stubState, handlerContext);

        Assert.That(((StubHandler)handler.Instance).HandledState, Is.EqualTo(stubState));
    }

    [Test]
    public async Task Should_have_passed_through_correct_context()
    {
        var cache = new MessageHandlerRegistry();
        cache.AddHandler<StubHandler>();

        var handler = cache.GetCachedHandlerForMessage<StubTimeoutState>();
        var handlerContext = new TestableMessageHandlerContext();
        await handler.Invoke(new StubTimeoutState(), handlerContext);

        Assert.That(((StubHandler)handler.Instance).HandlerContext, Is.SameAs(handlerContext));
    }

    public class StubHandler : IHandleTimeouts<StubTimeoutState>,
        IHandleMessages // Needed to meet generic constraint for AddHandler<THandler>()
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