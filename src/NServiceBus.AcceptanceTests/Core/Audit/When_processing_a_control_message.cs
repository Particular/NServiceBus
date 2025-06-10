namespace NServiceBus.AcceptanceTests.Audit;

using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using Features;
using MessageMutator;
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
                config.RegisterMessageMutator(new ControlMessageDetector(context));
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

        class ControlMessageDetector(Context context) : IMutateIncomingTransportMessages
        {
            public Task MutateIncoming(MutateIncomingTransportMessageContext transportMessage)
            {
                context.ControlMessageProcessed = true;
                return Task.CompletedTask;
            }
        }
    }

    class AuditSpyEndpoint : EndpointConfigurationBuilder
    {
        public AuditSpyEndpoint() => EndpointSetup<DefaultServer, Context>((config, context) => config.RegisterMessageMutator(new ControlMessageDetector(context)));

        class ControlMessageDetector(Context context) : IMutateIncomingTransportMessages
        {
            public Task MutateIncoming(MutateIncomingTransportMessageContext transportMessage)
            {
                context.ControlMessageAudited = true;
                return Task.CompletedTask;
            }
        }
    }
}