﻿namespace NServiceBus.AcceptanceTests.Core.UnitOfWork.TransactionScope;

using System.Threading.Tasks;
using System.Transactions;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_transactionscope_enabled : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_wrap_the_handlers_in_a_scope()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<ScopeEndpoint>(g => g.When(b => b.SendLocal(new MyMessage())))
            .Done(c => c.Done)
            .Run();

        Assert.That(context.AmbientTransactionPresent, Is.True, "There should be a ambient transaction present");
        Assert.That(context.IsolationLevel, Is.EqualTo(IsolationLevel.RepeatableRead), "There should be a ambient transaction present");
    }

    public class Context : ScenarioContext
    {
        public bool Done { get; set; }
        public bool AmbientTransactionPresent { get; set; }
        public IsolationLevel IsolationLevel { get; set; }
    }

    public class ScopeEndpoint : EndpointConfigurationBuilder
    {
        public ScopeEndpoint()
        {
            EndpointSetup<DefaultServer>((c, r) =>
            {
                c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                c.UnitOfWork()
                    .WrapHandlersInATransactionScope(
                        isolationLevel: IsolationLevel.RepeatableRead);
            });
        }

        class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public MyMessageHandler(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                if (Transaction.Current != null)
                {
                    testContext.AmbientTransactionPresent = Transaction.Current != null;
                    testContext.IsolationLevel = Transaction.Current.IsolationLevel;
                }
                testContext.Done = true;

                return Task.CompletedTask;
            }

            Context testContext;
        }
    }

    public class MyMessage : IMessage
    {
    }
}