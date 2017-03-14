namespace NServiceBus.AcceptanceTests.Tx.ImmediateDispatch
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_sending_inside_ambient_tx : NServiceBusAcceptanceTest
    {
        //This test is verifying the legacy behavior to immediately dispatch messages via suppressing the transaction scope
        //This test should be removed when the ForceImmediateDispatchForOperationsInSuppressedScopeBehavior behavior is removed
        [Test]
        public async Task Should_not_roll_the_message_back_to_the_queue_in_case_of_failure()
        {
            Requires.DtcSupport();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<NonTransactionalEndpoint>(b => b
                    .When(session => session.SendLocal(new MyMessage()))
                    .DoNotFailOnErrorMessages())
                .Done(c => c.TestComplete)
                .Run();

            Assert.False(context.MessageEnlistedInTheAmbientTxReceived, "The enlisted session.Send should not commit");
        }

        public class Context : ScenarioContext
        {
            public bool TestComplete { get; set; }

            public bool MessageEnlistedInTheAmbientTxReceived { get; set; }
        }

        public class NonTransactionalEndpoint : EndpointConfigurationBuilder
        {
            public NonTransactionalEndpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.ConfigureTransport().Transactions(TransportTransactionMode.None);
                    config.Pipeline.Register("WrapInScope", new WrapHandlersInScope(), "Wraps the handlers in a scope");
                });
            }

            class WrapHandlersInScope : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
            {
                public async Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
                {
                    using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                    {
                        await next(context).ConfigureAwait(false);
                        tx.Complete();
                    }
                }
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context TestContext { get; set; }

                public async Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    await context.SendLocal(new CompleteTest
                    {
                        EnlistedInTheAmbientTx = true
                    });

                    using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                    {
                        await context.SendLocal(new CompleteTest());
                    }

                    throw new SimulatedException();
                }
            }

            public class CompleteTestHandler : IHandleMessages<CompleteTest>
            {
                public Context Context { get; set; }

                public Task Handle(CompleteTest message, IMessageHandlerContext context)
                {
                    if (!Context.MessageEnlistedInTheAmbientTxReceived)
                    {
                        Context.MessageEnlistedInTheAmbientTxReceived = message.EnlistedInTheAmbientTx;
                    }

                    Context.TestComplete = true;

                    return Task.FromResult(0);
                }
            }
        }


        public class MyMessage : ICommand
        {
        }


        public class CompleteTest : ICommand
        {
            public bool EnlistedInTheAmbientTx { get; set; }
        }
    }
}