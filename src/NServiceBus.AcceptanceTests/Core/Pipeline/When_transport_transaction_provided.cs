namespace NServiceBus.AcceptanceTests.Core.Pipeline;

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
            .Run();

        Assert.That(context.DispatchPipelineTransportTransaction, Is.SameAs(context.IncomingPipelineTransportTransaction), "Transport Transaction was not the same");
    }

    public class Context : ScenarioContext
    {
        public TransportTransaction IncomingPipelineTransportTransaction { get; set; }
        public TransportTransaction DispatchPipelineTransportTransaction { get; set; }
        public bool SomeMessageReceived { get; set; }
    }

    public class ContextExtendingEndpoint : EndpointConfigurationBuilder
    {
        public ContextExtendingEndpoint() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.Pipeline.Register(nameof(IncomingTransportReceivePipelineBehavior), b => new IncomingTransportReceivePipelineBehavior(b.GetRequiredService<Context>()), "Tries to read the transport transaction in the incoming pipeline");
                c.Pipeline.Register(nameof(DispatchContextBehavior), b => new DispatchContextBehavior(b.GetRequiredService<Context>()), "Tries to read the transport transaction in the dispatch pipeline");
            });

        [Handler]
        public class SomeMessageHandler(Context testContext) : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                testContext.SomeMessageReceived = true;
                return context.SendLocal(new AnotherMessage());
            }
        }

        [Handler]
        public class AnotherMessageHandler(Context testContext) : IHandleMessages<AnotherMessage>
        {
            public Task Handle(AnotherMessage message, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }

        class IncomingTransportReceivePipelineBehavior(Context testContext) : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
        {
            public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
            {
                if (!testContext.SomeMessageReceived)
                {
                    testContext.IncomingPipelineTransportTransaction = context.Extensions.GetOrCreate<TransportTransaction>();
                }
                return next(context);
            }
        }

        class DispatchContextBehavior(Context testContext) : IBehavior<IDispatchContext, IDispatchContext>
        {
            public Task Invoke(IDispatchContext context, Func<IDispatchContext, Task> next)
            {
                if (context.Extensions.TryGet<IncomingMessage>(out _))
                {
                    testContext.DispatchPipelineTransportTransaction = context.Extensions.GetOrCreate<TransportTransaction>();
                }

                return next(context);
            }
        }
    }

    public class SomeMessage : ICommand;

    public class AnotherMessage : ICommand;
}