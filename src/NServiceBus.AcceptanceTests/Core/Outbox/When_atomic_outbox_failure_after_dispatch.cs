namespace NServiceBus.AcceptanceTests.Outbox;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_atomic_outbox_failure_after_dispatch : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Messages_are_dispatched_after_retry()
    {
        //HINT: The test scenario simulates a failure just before the incoming message is ACKed.
        //      This triggers a retry after which the outgoing messages stored in the outbox are dispatched
        //      It can happen because in the AtomicSendsWithReceive mode the removal of stored outbox messages
        //      does not happen in the outbox behavior but via a separate control message.
        Requires.OutboxPersistence();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<AtomicOutboxEndpoint>(b => b
                .When(session => session.SendLocal(new PlaceOrder()))
                .DoNotFailOnErrorMessages()
                .CustomConfig(cfg =>
                {
                    var recoverability = cfg.Recoverability();
                    recoverability.Immediate(immediate => immediate.NumberOfRetries(5));
                }))
            .WithEndpoint<DownstreamEndpoint>()
            .Done(c => c.SendOrderAcknowledgementReceived)
            .Run(TimeSpan.FromSeconds(20));

        Assert.That(context.SendOrderAcknowledgementReceived, Is.True);
    }

    [Test]
    public async Task Messages_are_not_dispatched_immediately()
    {
        //HINT: The test scenario simulates a failure just before the incoming message is ACKed
        //      The incoming message is moved to the error queue after which a follow-up message
        //      is generated. If the original message had been dispatched immediately, it would
        //      arrive before the follow-up one, failing the test.

        Requires.OutboxPersistence();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<AtomicOutboxEndpoint>(b => b
                .When(session => session.SendLocal(new PlaceOrder()))
                .When(ctx => ctx.MessageMovedToErrorQueue, session => session.SendLocal(new PlaceOrderFollowUp()))
                .DoNotFailOnErrorMessages()
                .CustomConfig(cfg =>
                {
                    var recoverability = cfg.Recoverability();
                    recoverability.Immediate(immediate => immediate.NumberOfRetries(0));
                    recoverability.Delayed(immediate => immediate.NumberOfRetries(0));
                }))
            .WithEndpoint<DownstreamEndpoint>()
            .WithEndpoint<ErrorSpy>()
            .Done(c => c.SendOrderAcknowledgementFollowUpReceived)
            .Run(TimeSpan.FromSeconds(20));

        Assert.That(context.SendOrderAcknowledgementReceived, Is.False);
    }

    class Context : ScenarioContext
    {
        public bool SendOrderAcknowledgementFollowUpReceived { get; set; }
        public bool SendOrderAcknowledgementReceived { get; set; }
        public bool MessageMovedToErrorQueue { get; set; }
    }

    public class AtomicOutboxEndpoint : EndpointConfigurationBuilder
    {
        public AtomicOutboxEndpoint()
        {
            EndpointSetup<DefaultServer>(b =>
            {
                b.EnableOutbox();
                b.LimitMessageProcessingConcurrencyTo(1);
                b.SendFailedMessagesTo(Conventions.EndpointNamingConvention(typeof(ErrorSpy)));
                b.GetSettings().Set("Outbox.AllowSendsAtomicWithReceive", true);
                b.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.SendsAtomicWithReceive;
                b.Pipeline.Register(new AfterDispatchFailureSimulatorBehavior(), "Simulates a failure after dispatching outbox operations");

                var routing = b.ConfigureRouting();
                routing.RouteToEndpoint(typeof(SendOrderAcknowledgement), typeof(DownstreamEndpoint));
                routing.RouteToEndpoint(typeof(SendOrderAcknowledgementFollowUp), typeof(DownstreamEndpoint));
            });
        }

        class AfterDispatchFailureSimulatorBehavior : Behavior<ITransportReceiveContext>
        {
            bool failureTriggered;

            public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
            {
                await next();
                if (!failureTriggered)
                {
                    failureTriggered = true;
                    throw new SimulatedException("Simulated failure after dispatch");
                }
            }
        }

        class PlaceOrderHandler : IHandleMessages<PlaceOrder>
        {
            public Task Handle(PlaceOrder message, IMessageHandlerContext context) => context.Send(new SendOrderAcknowledgement());
        }

        class PlaceOrderFollowUpHandler : IHandleMessages<PlaceOrderFollowUp>
        {
            public Task Handle(PlaceOrderFollowUp message, IMessageHandlerContext context) => context.Send(new SendOrderAcknowledgementFollowUp());
        }
    }

    public class DownstreamEndpoint : EndpointConfigurationBuilder
    {
        public DownstreamEndpoint()
        {
            EndpointSetup<DefaultServer>(b =>
            {
                b.LimitMessageProcessingConcurrencyTo(1);
            });
        }

        class SendOrderAcknowledgementFollowUpHandler(Context testContext)
            : IHandleMessages<SendOrderAcknowledgementFollowUp>
        {
            public Task Handle(SendOrderAcknowledgementFollowUp message, IMessageHandlerContext context)
            {
                testContext.SendOrderAcknowledgementFollowUpReceived = true;
                return Task.CompletedTask;
            }
        }

        class SendOrderAcknowledgementHandler(Context testContext)
            : IHandleMessages<SendOrderAcknowledgement>
        {
            public Task Handle(SendOrderAcknowledgement message, IMessageHandlerContext context)
            {
                testContext.SendOrderAcknowledgementReceived = true;
                return Task.CompletedTask;
            }
        }
    }

    class ErrorSpy : EndpointConfigurationBuilder
    {
        public ErrorSpy()
        {
            EndpointSetup<DefaultServer>(c => c.Pipeline.Register(typeof(ErrorMessageDetector), "Detect incoming error messages"));
        }

        class ErrorMessageDetector(Context testContext) : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
        {
            public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
            {
                testContext.MessageMovedToErrorQueue = true;
                return Task.CompletedTask;
            }
        }
    }

    public class PlaceOrder : ICommand
    {
    }

    public class PlaceOrderFollowUp : ICommand
    {
    }

    public class SendOrderAcknowledgement : IMessage
    {
    }

    public class SendOrderAcknowledgementFollowUp : IMessage
    {
    }
}