namespace NServiceBus.Core.Tests.Routing;

using System;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using NUnit.Framework;
using Testing;
using Transport;
using Unicast.Messages;

[TestFixture]
public class NativeSubscribeTerminatorTests
{
    [Test]
    public void When_subscriptionmanager_throws_aggregateexception_on_subscribe()
    {
        var innerException = new Exception("expected exception");
        var fakeSubscriptionManager = new FakeSubscriptionManager(new AggregateException(innerException));
        var messageMetadataRegistry = new MessageMetadataRegistry();
        messageMetadataRegistry.Initialize(_ => true, true);
        var terminator =
            new NativeSubscribeTerminator(fakeSubscriptionManager, messageMetadataRegistry);

        var exception = Assert.ThrowsAsync<Exception>(() => terminator.Invoke(new TestableSubscribeContext(), _ => Task.CompletedTask));

        Assert.That(exception, Is.SameAs(innerException));
    }

    [Test]
    public void When_subscriptionmanager_throws_exception_on_subscribe()
    {
        var expectedException = new Exception("expected exception");
        var fakeSubscriptionManager = new FakeSubscriptionManager(expectedException);
        var messageMetadataRegistry = new MessageMetadataRegistry();
        messageMetadataRegistry.Initialize(_ => true, true);
        var terminator = new NativeSubscribeTerminator(fakeSubscriptionManager, messageMetadataRegistry);

        var exception = Assert.ThrowsAsync<Exception>(() => terminator.Invoke(new TestableSubscribeContext(), _ => Task.CompletedTask));

        Assert.That(exception, Is.SameAs(expectedException));
    }

    [Test]
    public void When_subscriptionmanager_throws_aggregateexception_on_subscribeAll()
    {
        var aggregateException = new AggregateException(new Exception("expected exception"));
        var fakeSubscriptionManager = new FakeSubscriptionManager(aggregateException);
        var messageMetadataRegistry = new MessageMetadataRegistry();
        messageMetadataRegistry.Initialize(_ => true, true);
        var terminator = new NativeSubscribeTerminator(fakeSubscriptionManager, messageMetadataRegistry);
        var testableSubscribeContext = new TestableSubscribeContext();
        testableSubscribeContext.Extensions.Set(MessageSession.SubscribeAllFlagKey, true);

        var exception = Assert.ThrowsAsync<AggregateException>(() => terminator.Invoke(testableSubscribeContext, _ => Task.CompletedTask));

        Assert.That(exception, Is.SameAs(aggregateException));
    }

    [Test]
    public void When_subscriptionmanager_throws_exception_on_subscribeAll()
    {
        var expectedException = new Exception("expected exception");
        var fakeSubscriptionManager = new FakeSubscriptionManager(expectedException);
        var messageMetadataRegistry = new MessageMetadataRegistry();
        messageMetadataRegistry.Initialize(_ => true, true);
        var terminator = new NativeSubscribeTerminator(fakeSubscriptionManager, messageMetadataRegistry);
        var testableSubscribeContext = new TestableSubscribeContext();
        testableSubscribeContext.Extensions.Set(MessageSession.SubscribeAllFlagKey, true);

        var exception = Assert.ThrowsAsync<Exception>(() => terminator.Invoke(testableSubscribeContext, _ => Task.CompletedTask));

        Assert.That(exception, Is.SameAs(expectedException));
    }

    class FakeSubscriptionManager(Exception exceptionToThrow) : ISubscriptionManager
    {
        public Task SubscribeAll(MessageMetadata[] eventTypes, ContextBag context, CancellationToken cancellationToken = default) => throw exceptionToThrow;

        public Task Unsubscribe(MessageMetadata eventType, ContextBag context, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}