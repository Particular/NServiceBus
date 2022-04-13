namespace NServiceBus.Unicast.Tests
{
    using Outbox;
    using NServiceBus.Transport;
    using NUnit.Framework;
    using Persistence;
    using Testing;

    [TestFixture]
    public class LoadHandlersBehaviorTests
    {
        [Test]
        public void Should_throw_when_there_are_no_registered_message_handlers()
        {
            var behavior = new LoadHandlersConnector(new MessageHandlerRegistry());

            var context = new TestableIncomingLogicalMessageContext();

            context.Extensions.Set<OutboxTransaction>(new InMemoryOutboxTransaction());
            context.Extensions.Set(new TransportTransaction());
            context.Builder.Register(() => new CompletableSynchronizedStorageSessionAdapter(new InMemoryTransactionalSynchronizedStorageAdapter(), new InMemorySynchronizedStorage()));

            Assert.That(async () => await behavior.Invoke(context, c => TaskEx.CompletedTask), Throws.InvalidOperationException);
        }
    }
}