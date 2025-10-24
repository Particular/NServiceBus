namespace NServiceBus.Unicast.Tests;

using System;
using System.Threading.Tasks;
using System.Transactions;
using Core.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Transport;
using NUnit.Framework;
using Outbox;
using Testing;

[TestFixture]
public class LoadHandlersConnectorTests
{
    [Test]
    public void Should_throw_when_there_are_no_registered_message_handlers()
    {
        var behavior = new LoadHandlersConnector(new MessageHandlerRegistry(), new NoOpActivityFactory());

        var context = new TestableIncomingLogicalMessageContext();

        context.Extensions.Set<IOutboxTransaction>(new FakeOutboxTransaction());
        context.Extensions.Set(new TransportTransaction());

        Assert.That(async () => await behavior.Invoke(context, c => Task.CompletedTask), Throws.InvalidOperationException);
    }

    [Test]
    public void Should_throw_if_ambient_transaction_is_different_from_scope_used_by_transport()
    {
        var behavior = new LoadHandlersConnector(new MessageHandlerRegistry(), new NoOpActivityFactory());

        var context = new TestableIncomingLogicalMessageContext();

        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            context.Extensions.Set(CreateTransactionScopeModeTransportTransaction());

            using (new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
            {
                var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await behavior.Invoke(context, c => Task.CompletedTask));

                Assert.That(ex.Message, Does.Contain("A TransactionScope has been created that is overriding the one created by the transport"));
            }
        }
    }

    [Test]
    public void Should_throw_if_ambient_transaction_suppressed_when_transport_uses_a_scope()
    {
        var behavior = new LoadHandlersConnector(new MessageHandlerRegistry(), new NoOpActivityFactory());

        var context = new TestableIncomingLogicalMessageContext();

        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            context.Extensions.Set(CreateTransactionScopeModeTransportTransaction());

            using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            {
                var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await behavior.Invoke(context, c => Task.CompletedTask));

                Assert.That(ex.Message, Does.Contain("The TransactionScope created by the transport has been suppressed"));
            }
        }
    }

    [Test]
    public void Should_not_throw_if_ambient_scope_is_same_as_transport_scope()
    {
        var messageHandlerRegistry = new MessageHandlerRegistry();
        messageHandlerRegistry.RegisterHandler<FakeHandler>();

        var context = new TestableIncomingLogicalMessageContext();

        context.Services.AddSingleton<FakeHandler>();
        context.Extensions.Set<IOutboxTransaction>(new NoOpOutboxTransaction());

        var behavior = new LoadHandlersConnector(messageHandlerRegistry, new NoOpActivityFactory());

        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            context.Extensions.Set(CreateTransactionScopeModeTransportTransaction());

            using (new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
            {
                Assert.DoesNotThrowAsync(async () => await behavior.Invoke(context, c => Task.CompletedTask));
            }
        }
    }

    static TransportTransaction CreateTransactionScopeModeTransportTransaction()
    {
        var transportTransaction = new TransportTransaction();

        transportTransaction.Set(Transaction.Current);

        return transportTransaction;
    }

    class FakeHandler : IHandleMessages<object>
    {
        public Task Handle(object message, IMessageHandlerContext context) => Task.CompletedTask;
    }
}