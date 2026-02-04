namespace NServiceBus.AcceptanceTests.Core.Recoverability;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Support;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Transport;

public class When_transport_transaction_provided : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_be_available_in_pipeline()
    {
        var exception = Assert.ThrowsAsync<MessageFailedException>(async () => await Scenario.Define<Context>()
            .WithEndpoint<ContextExtendingEndpoint>(e => e
                .CustomConfig(config =>
                {
                    config.Recoverability().AddUnrecoverableException<SimulatedException>();
                })
                .When((session, _) => session.SendLocal(new SomeMessage())))
            .Run());

        var context = (Context)exception.ScenarioContext;
        Assert.That(context.DispatchPipelineTransportTransaction, Is.SameAs(context.IncomingPipelineTransportTransaction), "Transport Transaction was not the same");
    }

    public class Context : ScenarioContext
    {
        public TransportTransaction IncomingPipelineTransportTransaction { get; set; }
        public TransportTransaction DispatchPipelineTransportTransaction { get; set; }
    }

    public class ContextExtendingEndpoint : EndpointConfigurationBuilder
    {
        public ContextExtendingEndpoint() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.Pipeline.Register(nameof(IncomingTransportReceivePipelineBehavior), b => new IncomingTransportReceivePipelineBehavior(b.GetRequiredService<Context>()), "Tries to read the transport transaction in the incoming pipeline");
                c.Pipeline.Register(nameof(RecoverabilityContextBehavior), b => new RecoverabilityContextBehavior(b.GetRequiredService<Context>()), "Tries to read the transport transaction in the recoverability pipeline");
            });

        [Handler]
        public class SomeMessageHandler : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context) => throw new SimulatedException();
        }

        class IncomingTransportReceivePipelineBehavior(Context testContext) : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
        {
            public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
            {
                testContext.IncomingPipelineTransportTransaction = context.Extensions.GetOrCreate<TransportTransaction>();
                return next(context);
            }
        }

        class RecoverabilityContextBehavior(Context testContext) : IBehavior<IRecoverabilityContext, IRecoverabilityContext>
        {
            public Task Invoke(IRecoverabilityContext context, Func<IRecoverabilityContext, Task> next)
            {
                testContext.DispatchPipelineTransportTransaction = context.Extensions.GetOrCreate<TransportTransaction>();
                return next(context);
            }
        }
    }

    public class SomeMessage : ICommand;
}