namespace NServiceBus.Unicast.Tests
{
    using System.Threading.Tasks;
    using Outbox;
    using NServiceBus.Transport;
    using NUnit.Framework;
    using Testing;
    using Core.Tests.Fakes;
    using Microsoft.Extensions.DependencyInjection;
    using Persistence;

    [TestFixture]
    public class LoadHandlersConnectorTests
    {
        [Test]
        public void Should_throw_when_there_are_no_registered_message_handlers()
        {
            var behavior = new LoadHandlersConnector(new MessageHandlerRegistry(), new FakeSynchronizedStorage(),
                new FakeTransactionalSynchronizedStorageAdapter());

            var context = new TestableIncomingLogicalMessageContext();

            context.Extensions.Set<IOutboxTransaction>(new FakeOutboxTransaction());
            context.Extensions.Set(new TransportTransaction());

            Assert.That(async () => await behavior.Invoke(context, c => Task.CompletedTask),
                Throws.InvalidOperationException);
        }

        [Test]
        public async Task Should_not_have_synchronized_session_available_after_handlers_connector_completes()
        {
            var behavior = new LoadHandlersConnector(new MessageHandlerRegistry(), new FakeSynchronizedStorage(),
                new FakeTransactionalSynchronizedStorageAdapter());

            var context = new TestableIncomingLogicalMessageContext();
            context.Extensions.Set<IOutboxTransaction>(new FakeOutboxTransaction());
            context.Extensions.Set(new TransportTransaction());
            context.MessageHandled = true;

            var synchronizedStorageSessionProvider = new SynchronizedStorageSessionProvider();
            context.Services.AddSingleton(synchronizedStorageSessionProvider);

            await behavior.Invoke(context, c =>
            {
                var capturedSession = synchronizedStorageSessionProvider.SynchronizedStorageSession;
                Assert.NotNull(capturedSession);
                return Task.CompletedTask;
            });

            Assert.IsNull(synchronizedStorageSessionProvider.SynchronizedStorageSession);
        }
    }
}