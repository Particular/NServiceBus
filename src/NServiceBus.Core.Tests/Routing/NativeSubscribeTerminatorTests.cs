namespace NServiceBus.Core.Tests.Routing
{
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
            var terminator =
                new NativeSubscribeTerminator(fakeSubscriptionManager, new MessageMetadataRegistry(_ => true));

            var exception = Assert.ThrowsAsync<Exception>(() => terminator.Invoke(new TestableSubscribeContext(), _ => Task.CompletedTask));

            Assert.AreSame(innerException, exception);
        }

        [Test]
        public void When_subscriptionmanager_throws_exception_on_subscribe()
        {
            var expectedException = new Exception("expected exception");
            var fakeSubscriptionManager = new FakeSubscriptionManager(expectedException);
            var terminator =
                new NativeSubscribeTerminator(fakeSubscriptionManager, new MessageMetadataRegistry(_ => true));

            var exception = Assert.ThrowsAsync<Exception>(() => terminator.Invoke(new TestableSubscribeContext(), _ => Task.CompletedTask));

            Assert.AreSame(expectedException, exception);
        }

        [Test]
        public void When_subscriptionmanager_throws_aggregateexception_on_subscribeAll()
        {
            var aggregateException = new AggregateException(new Exception("expected exception"));
            var fakeSubscriptionManager = new FakeSubscriptionManager(aggregateException);
            var terminator =
                new NativeSubscribeTerminator(fakeSubscriptionManager, new MessageMetadataRegistry(_ => true));
            var testableSubscribeContext = new TestableSubscribeContext();
            testableSubscribeContext.Extensions.Set(MessageSession.SubscribeAllFlagKey, true);

            var exception = Assert.ThrowsAsync<AggregateException>(() => terminator.Invoke(testableSubscribeContext, _ => Task.CompletedTask));

            Assert.AreSame(aggregateException, exception);
        }

        [Test]
        public void When_subscriptionmanager_throws_exception_on_subscribeAll()
        {
            var expectedException = new Exception("expected exception");
            var fakeSubscriptionManager = new FakeSubscriptionManager(expectedException);
            var terminator =
                new NativeSubscribeTerminator(fakeSubscriptionManager, new MessageMetadataRegistry(_ => true));
            var testableSubscribeContext = new TestableSubscribeContext();
            testableSubscribeContext.Extensions.Set(MessageSession.SubscribeAllFlagKey, true);

            var exception = Assert.ThrowsAsync<Exception>(() => terminator.Invoke(testableSubscribeContext, _ => Task.CompletedTask));

            Assert.AreSame(expectedException, exception);
        }

        class FakeSubscriptionManager : ISubscriptionManager
        {
            Exception exceptionToThrow;

            public FakeSubscriptionManager(Exception exceptionToThrow)
            {
                this.exceptionToThrow = exceptionToThrow;
            }

            public Task SubscribeAll(MessageMetadata[] eventTypes, ContextBag context, CancellationToken cancellationToken) => throw exceptionToThrow;

            public Task Unsubscribe(MessageMetadata eventType, ContextBag context, CancellationToken cancellationToken) => throw new NotImplementedException();
        }
    }
}