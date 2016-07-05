﻿namespace NServiceBus.AcceptanceTests
{
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_customizing_scope_isolation_level : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_honor_configured_level()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<ScopeEndpoint>(g => g.When(b => b.SendLocal(new MyMessage())))
                    .Done(c => c.Done)
                    .Run();

            Assert.True(context.AmbientTransactionPresent, "There should be a ambient transaction present");
            Assert.AreEqual(context.IsolationLevel, IsolationLevel.RepeatableRead, "There should be a ambient transaction present");
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
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<MsmqTransport>()
                        .Transactions(TransportTransactionMode.TransactionScope)
                        .TransactionScopeOptions(isolationLevel: IsolationLevel.RepeatableRead);
                });
            }

            class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    if (Transaction.Current != null)
                    {
                        Context.AmbientTransactionPresent = Transaction.Current != null;
                        Context.IsolationLevel = Transaction.Current.IsolationLevel;
                    }
                    Context.Done = true;

                    return Task.FromResult(0);
                }
            }
        }

        class MyMessage : IMessage
        { }
    }
}