namespace NServiceBus.AcceptanceTests.Audit;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using Features;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NServiceBus.Transport;
using NUnit.Framework;
using Unicast.Transport;

public class When_processing_a_control_message : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_be_audited()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointProcessingControlMessage>()
            .WithEndpoint<AuditSpyEndpoint>()
            .Done(c => c.ControlMessageAudited)
            .Run();

        Assert.That(context.ControlMessageProcessed, Is.True);
        Assert.That(context.ControlMessageAudited, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool ControlMessageAudited { get; set; }
        public bool ControlMessageProcessed { get; set; }
    }

    public class EndpointProcessingControlMessage : EndpointConfigurationBuilder
    {
        public EndpointProcessingControlMessage() =>
            EndpointSetup<DefaultServer, Context>((config, context) =>
            {
                config.RegisterStartupTask<ControlMessageSender>();
                config.Pipeline.Register(new ControlMessageDetector(context), "Detects the processing of the control message");
                config.AuditProcessedMessagesTo<AuditSpyEndpoint>();
            });

        class ControlMessageSender(IMessageDispatcher dispatcher) : FeatureStartupTask
        {
            protected override async Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
            {
                var controlMessage = ControlMessageFactory.Create(MessageIntent.Send);
                var messageOperation = new TransportOperation(controlMessage, new UnicastAddressTag(Conventions.EndpointNamingConvention(typeof(EndpointProcessingControlMessage))));
                await dispatcher.Dispatch(new TransportOperations(messageOperation), new TransportTransaction(), cancellationToken);
            }

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        class ControlMessageDetector(Context testContext) : Behavior<IIncomingPhysicalMessageContext>
        {
            public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
            {
                testContext.ControlMessageProcessed = true;
                return next();
            }
        }
    }

    class AuditSpyEndpoint : EndpointConfigurationBuilder
    {
        public AuditSpyEndpoint() => EndpointSetup<DefaultServer, Context>((config, context) =>
            config.Pipeline.Register(new ControlMessageDetector(context), "Detects the auditing of the control message"));

        class ControlMessageDetector(Context testContext) : Behavior<IIncomingPhysicalMessageContext>
        {
            public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
            {
                testContext.ControlMessageAudited = true;
                return next();
            }
        }
    }
}