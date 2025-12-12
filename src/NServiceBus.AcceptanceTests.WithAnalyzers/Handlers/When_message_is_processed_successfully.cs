namespace NServiceBus.AcceptanceTests.Handlers;

using System.Threading.Tasks;
using EndpointTemplates;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

[NServiceBusRegistrations]
public class When_manually_registering_handler_with_complex_hierarchy : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_handle_message_with_manually_registered_handler()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithComplexMessageHierarchy>(b =>
                b.When(async (session, _) => await session.SendLocal(new OutgoingWithComplexHierarchyMessage())))
            .Done(c => c.ComplexOutgoingMessageReceived)
            .Run();

        Assert.That(context.ComplexOutgoingMessageReceived, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool ComplexOutgoingMessageReceived;
    }

    public class EndpointWithComplexMessageHierarchy : EndpointConfigurationBuilder
    {
        public EndpointWithComplexMessageHierarchy() => EndpointSetup<NonScanningServer>(config =>
        {
            config.AddHandler<ComplexMessageHandler>();
        });

        public class ComplexMessageHandler(Context testContext) : IHandleMessages<OutgoingWithComplexHierarchyMessage>
        {
            public Task Handle(OutgoingWithComplexHierarchyMessage message, IMessageHandlerContext context)
            {
                testContext.ComplexOutgoingMessageReceived = true;
                return Task.CompletedTask;
            }
        }
    }

    public class BaseBaseOutgoingMessage : IMessage;

    public class BaseOutgoingMessage : BaseBaseOutgoingMessage;

    public class OutgoingWithComplexHierarchyMessage : BaseOutgoingMessage;
}