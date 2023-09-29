namespace NServiceBus.Unicast.Tests
{
    using System.Threading.Tasks;
    using Outbox;
    using NServiceBus.Transport;
    using NUnit.Framework;
    using Testing;
    using Core.Tests.Fakes;
    using System.Transactions;
    using System;
    using Microsoft.Extensions.DependencyInjection;

    [TestFixture]
    public class LoadHandlersConnectorTests
    {
        [Test]
        public void Should_throw_when_there_are_no_registered_message_handlers()
        {
            var behavior = new LoadHandlersConnector(new MessageHandlerRegistry());

            var context = new TestableIncomingLogicalMessageContext();

            context.Extensions.Set<IOutboxTransaction>(new FakeOutboxTransaction());
            context.Extensions.Set(new TransportTransaction());

            Assert.That(async () => await behavior.Invoke(context, c => Task.CompletedTask), Throws.InvalidOperationException);
        }

        [Test]
        public void Should_throw_if_scope_in_transport_transaction_differs_from_the_ambient()
        {
            var behavior = new LoadHandlersConnector(new MessageHandlerRegistry());

            var context = new TestableIncomingLogicalMessageContext();

            context.Extensions.Set<IOutboxTransaction>(new NoOpOutboxTransaction());

            using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var transportTransaction = new TransportTransaction();

                transportTransaction.Set(Transaction.Current);

                context.Extensions.Set(transportTransaction);

                using (new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
                {
                    var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await behavior.Invoke(context, c => Task.CompletedTask));

                    StringAssert.Contains("A TransactionScope has been opened in the current context overriding the one created by the transport", ex.Message);
                }
            }
        }

        [Test]
        public void Should_not_throw_if_scope_in_transport_transaction_are_the_same_as_the_ambient()
        {
            var messageHandlerRegistry = new MessageHandlerRegistry();
            messageHandlerRegistry.RegisterHandler(typeof(FakeHandler));

            var context = new TestableIncomingLogicalMessageContext();

            context.Services.AddSingleton<FakeHandler>();
            context.Extensions.Set<IOutboxTransaction>(new NoOpOutboxTransaction());

            var behavior = new LoadHandlersConnector(messageHandlerRegistry);

            using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var transportTransaction = new TransportTransaction();

                transportTransaction.Set(Transaction.Current);

                context.Extensions.Set(transportTransaction);

                using (new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
                {
                    Assert.DoesNotThrowAsync(async () => await behavior.Invoke(context, c => Task.CompletedTask));
                }
            }
        }

        class FakeHandler : IHandleMessages<object>
        {
            public Task Handle(object message, IMessageHandlerContext context) => Task.CompletedTask;
        }
    }
}