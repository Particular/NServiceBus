namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
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
        public async Task Should_be_available_during_dispatch()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<ContextExtendingEndpoint>(e => e
                    .When((session, _) => session.SendLocal(new SomeMessage())))
                .Done(c => c.AnotherMessageReceived)
                .Run();

            Assert.AreSame(context.IncomingPipelineTransportTransaction, context.DispatchPipelineTransportTransaction, "Transport Transaction was not the same");
        }

        class Context : ScenarioContext
        {
            public TransportTransaction IncomingPipelineTransportTransaction { get; set; }
            public TransportTransaction DispatchPipelineTransportTransaction { get; set; }
            public bool AnotherMessageReceived { get; set; }
            public bool SomeMessageReceived { get; set; }
        }

        class ContextExtendingEndpoint : EndpointConfigurationBuilder
        {
            public ContextExtendingEndpoint() =>
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Pipeline.Register(nameof(IncomingTransportReceivePipelineBehavior), b => new IncomingTransportReceivePipelineBehavior(b.GetRequiredService<Context>()), "Tries to read the transport transaction in the incoming pipeline");
                    c.Pipeline.Register(nameof(DispatchContextBehavior), b => new DispatchContextBehavior(b.GetRequiredService<Context>()), "Tries to read the transport transaction in the dispatch pipeline");
                });

            class SomeMessageHandler : IHandleMessages<SomeMessage>
            {
                public SomeMessageHandler(Context context) => testContext = context;

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    testContext.SomeMessageReceived = true;
                    return context.SendLocal(new AnotherMessage());
                }

                Context testContext;
            }

            class AnotherMessageHandler : IHandleMessages<AnotherMessage>
            {
                public AnotherMessageHandler(Context context) => testContext = context;

                public Task Handle(AnotherMessage message, IMessageHandlerContext context)
                {
                    testContext.AnotherMessageReceived = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }

            class IncomingTransportReceivePipelineBehavior : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
            {
                public IncomingTransportReceivePipelineBehavior(Context testContext) => this.testContext = testContext;

                public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
                {
                    if (!testContext.SomeMessageReceived)
                    {
                        testContext.IncomingPipelineTransportTransaction = context.Extensions.GetOrCreate<TransportTransaction>();
                    }
                    return next(context);
                }

                readonly Context testContext;
            }

            class DispatchContextBehavior : IBehavior<IDispatchContext, IDispatchContext>
            {
                public DispatchContextBehavior(Context testContext) => this.testContext = testContext;

                public Task Invoke(IDispatchContext context, Func<IDispatchContext, Task> next)
                {
                    if (context.Extensions.TryGet<IncomingMessage>(out _))
                    {
                        testContext.DispatchPipelineTransportTransaction = context.Extensions.GetOrCreate<TransportTransaction>();
                    }

                    return next(context);
                }

                readonly Context testContext;
            }
        }

        public class SomeMessage : ICommand
        {
        }

        public class AnotherMessage : ICommand
        {
        }
    }
}