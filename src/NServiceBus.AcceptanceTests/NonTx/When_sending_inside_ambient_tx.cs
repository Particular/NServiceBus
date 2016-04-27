namespace NServiceBus.AcceptanceTests.NonTx
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_sending_inside_ambient_tx : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_not_roll_the_message_back_to_the_queue_in_case_of_failure()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<NonTransactionalEndpoint>(b => b
                    .When(session => session.SendLocal(new MyMessage()))
                    .DoNotFailOnErrorMessages())
                .Done(c => c.TestComplete)
                .Repeat(r => r.For<AllDtcTransports>())
                .Should(c => Assert.False(c.MessageEnlistedInTheAmbientTxReceived, "The enlisted session.Send should not commit"))
                .Run();
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
                    config.UseTransport(context.GetTransportType()).Transactions(TransportTransactionMode.None);
                    config.Pipeline.Register("WrapInScope", typeof(WrapHandlersInScope), "Wraps the handlers in a scope");
                });
            }

            class WrapHandlersInScope : Behavior<IIncomingLogicalMessageContext>
            {
                public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
                {
                    using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                    {
                        await next();
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

        [Serializable]
        public class MyMessage : ICommand
        {
        }

        [Serializable]
        public class CompleteTest : ICommand
        {
            public bool EnlistedInTheAmbientTx { get; set; }
        }
    }
}