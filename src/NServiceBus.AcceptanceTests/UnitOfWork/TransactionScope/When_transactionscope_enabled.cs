namespace NServiceBus.AcceptanceTests.UnitOfWork
{
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_transactionscope_enabled
    {
        [Test]
        public async Task Should_wrap_the_handlers_in_a_scope()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<ScopeEndpoint>(g => g.When(b => b.SendLocal(new MyMessage())))
                    .Done(c => c.Done)
                    .Run();

            Assert.True(context.AmbientTransactionPresent, "There should be a ambient transaction present");
            Assert.AreEqual(context.IsolationLevel,IsolationLevel.RepeatableRead, "There should be a ambient transaction present");
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
                    c.UseTransport(r.GetTransportType())
                        .Transactions(TransportTransactionMode.ReceiveOnly);
                    c.UnitOfWork()
                        .WrapHandlersInATransactionScope(
                                isolationLevel: IsolationLevel.RepeatableRead);
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