namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Outbox;
    using NServiceBus.Transports;
    using Unicast.Messages;
    using NUnit.Framework;

    [TestFixture]
    public class LoadHandlersBehaviorTests
    {
        [Test]
        public void Should_throw_when_there_are_no_registered_message_handlers()
        {
            var behavior = new LoadHandlersConnector(new MessageHandlerRegistry(new Conventions()), new InMemorySynchronizedStorage(), new InMemoryTransactionalSynchronizedStorageAdapter());

            var context = new IncomingLogicalMessageContext(
                new LogicalMessage(new MessageMetadata(typeof(string)), null, null), 
                "messageId",
                "replyToAddress",
                new Dictionary<string, string>(), 
                null);

            context.Extensions.Set<OutboxTransaction>(new InMemoryOutboxTransaction());
            context.Extensions.Set<TransportTransaction>(new FakeTransportTransaction());

            Assert.Throws<InvalidOperationException>(async () => await behavior.Invoke(context, c => Task.FromResult(0)));
        }

        private class FakeTransportTransaction : TransportTransaction
        {
        }
    }
}