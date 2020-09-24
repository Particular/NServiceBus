namespace NServiceBus.Unicast.Tests
{
    using System.Threading.Tasks;
    using Outbox;
    using NServiceBus.Transport;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class LoadHandlersBehaviorTests
    {
        [Test]
        public void Should_throw_when_there_are_no_registered_message_handlers()
        {
          //TODO: Should be reviewed after InMemory move 
            //var behavior = new LoadHandlersConnector(new MessageHandlerRegistry(), new InMemorySynchronizedStorage(), new InMemoryTransactionalSynchronizedStorageAdapter());

            //var context = new TestableIncomingLogicalMessageContext();

            //context.Extensions.Set<OutboxTransaction>(new InMemoryOutboxTransaction());
            //context.Extensions.Set(new TransportTransaction());

            //Assert.That(async () => await behavior.Invoke(context, c => Task.CompletedTask), Throws.InvalidOperationException);
        }
    }
}