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
                b.When(async (session, _) => await session.SendLocal(new ComplexMessage())))
            .Done(c => c.ComplexMessageReceived)
            .Run();

        Assert.That(context.ComplexMessageReceived, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool ComplexMessageReceived;
    }

    public class EndpointWithComplexMessageHierarchy : EndpointConfigurationBuilder
    {
        public EndpointWithComplexMessageHierarchy() => EndpointSetup<NonScanningServer>(config =>
        {
            config.Handlers.NServiceBus_AcceptanceTests_WithAnalyzers.AcceptanceTests.Handlers.AddComplexMessage();
        });

        [Handler]
        public class ComplexMessageHandler(Context testContext) : IHandleMessages<ComplexMessage>
        {
            public Task Handle(ComplexMessage message, IMessageHandlerContext context)
            {
                testContext.ComplexMessageReceived = true;
                return Task.CompletedTask;
            }
        }
    }

    public class ComplexMessage : ConcreteParent1, IInterfaceParent1;
    public class ConcreteParent1 : ConcreteParentBase;
    public class ConcreteParentBase : IMessage;
    public interface IInterfaceParent1 : IInterfaceParent1Base;
    public interface IInterfaceParent1Base : IMessage;
}