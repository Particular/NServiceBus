namespace NServiceBus.AcceptanceTests.Core.Recoverability
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Transport;

    public class When_transport_transaction_provided : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_available_in_pipeline()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<ContextExtendingEndpoint>(e => e
                    .DoNotFailOnErrorMessages()
                    .CustomConfig(config =>
                    {
                        config.Recoverability().AddUnrecoverableException<SimulatedException>();
                    })
                    .When((session, _) => session.SendLocal(new SomeMessage())))
                .Done(c => c.FailedMessages.Any())
                .Run();

            Assert.AreSame(context.IncomingPipelineTransportTransaction, context.DispatchPipelineTransportTransaction, "Transport Transaction was not the same");
        }

        class Context : ScenarioContext
        {
            public TransportTransaction IncomingPipelineTransportTransaction { get; set; }
            public TransportTransaction DispatchPipelineTransportTransaction { get; set; }
        }

        class ContextExtendingEndpoint : EndpointConfigurationBuilder
        {
            public ContextExtendingEndpoint() =>
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Pipeline.Register(nameof(IncomingTransportReceivePipelineBehavior), b => new IncomingTransportReceivePipelineBehavior(b.GetRequiredService<Context>()), "Tries to read the transport transaction in the incoming pipeline");
                    c.Pipeline.Register(nameof(RecoverabilityContextBehavior), b => new RecoverabilityContextBehavior(b.GetRequiredService<Context>()), "Tries to read the transport transaction in the recoverability pipeline");
                });

            class SomeMessageHandler : IHandleMessages<SomeMessage>
            {
                public Task Handle(SomeMessage message, IMessageHandlerContext context) => throw new SimulatedException();
            }

            class IncomingTransportReceivePipelineBehavior : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
            {
                public IncomingTransportReceivePipelineBehavior(Context testContext) => this.testContext = testContext;

                public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
                {
                    testContext.IncomingPipelineTransportTransaction = context.Extensions.GetOrCreate<TransportTransaction>();
                    return next(context);
                }

                readonly Context testContext;
            }

            class RecoverabilityContextBehavior : IBehavior<IRecoverabilityContext, IRecoverabilityContext>
            {
                public RecoverabilityContextBehavior(Context testContext) => this.testContext = testContext;

                public Task Invoke(IRecoverabilityContext context, Func<IRecoverabilityContext, Task> next)
                {
                    testContext.DispatchPipelineTransportTransaction = context.Extensions.GetOrCreate<TransportTransaction>();
                    return next(context);
                }

                readonly Context testContext;
            }
        }

        public class SomeMessage : ICommand
        {
        }
    }
}