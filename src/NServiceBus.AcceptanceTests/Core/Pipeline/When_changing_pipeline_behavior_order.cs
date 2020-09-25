namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_changing_pipeline_behavior_order : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_invoke_replacement_in_pipeline()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithReplacement>(b => b.CustomConfig(c =>
                {
                    c.ConfigureTransport().Transactions(TransportTransactionMode.SendsAtomicWithReceive);
                    c.UnitOfWork().WrapHandlersInATransactionScope();
                })
                .When(s => s.SendLocal<Message>(m => { })))
                .Done(d => d.MessageHandled)
                .Run();

            Assert.IsTrue(context.TransactionFoundAndSuppressed);
            Assert.IsFalse(context.NoCurrentTransactionWhenSuppressing);
        }

        class Context : ScenarioContext
        {
            public bool TransactionFoundAndSuppressed { get; set; }
            public bool NoCurrentTransactionWhenSuppressing { get; set; }
            public bool MessageHandled { get; set; }
        }

        class TransactionScopeSuppressBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
        {
            public TransactionScopeSuppressBehavior(Context testContext)
            {
                this.testContext = testContext;
            }

            public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
            {
                if (Transaction.Current != null)
                {
                    using (var tx = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                    {
                        testContext.TransactionFoundAndSuppressed = true;
                        await next(context).ConfigureAwait(false);

                        tx.Complete();
                    }
                }
                else
                {
                    testContext.NoCurrentTransactionWhenSuppressing = true;
                    await next(context).ConfigureAwait(false);
                }
            }

            Context testContext;

            public class Registration : RegisterStep
            {
                public Registration() : base("HandlerTransactionScopeSuppressWrapper", typeof(TransactionScopeSuppressBehavior), "Makes sure that the handlers gets wrapped in a suppress transaction scope, preventing the ASB transaction scope from promoting")
                {
                    InsertBefore("ExecuteUnitOfWork");
                }
            }
        }

        class ReceiveBehavior : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
        {
            public async Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
            {
                using (new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.Serializable,
                    Timeout = TransactionManager.MaximumTimeout
                }, TransactionScopeAsyncFlowOption.Enabled))
                {
                    await next(context);
                }
            }
        }

        class EndpointWithReplacement : EndpointConfigurationBuilder
        {
            public EndpointWithReplacement()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Pipeline.Register("receive", new ReceiveBehavior(), "receive behavior");
                    c.Pipeline.Register(new TransactionScopeSuppressBehavior.Registration());
                });
            }

            public class Handler : IHandleMessages<Message>
            {
                readonly Context testContext;

                public Handler(Context testContext)
                {
                    this.testContext = testContext;
                }
                public Task Handle(Message message, IMessageHandlerContext context)
                {
                    testContext.MessageHandled = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class Message : IMessage
        {
        }
    }
}