namespace NServiceBus.AcceptanceTests.Handlers;

using System.Threading.Tasks;
using EndpointTemplates;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class When_registering_handler_with_complex_hierarchy_using_registry : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_handle_message()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointUsingRegistry>(b =>
                b.When(async (session, _) => await session.SendLocal(new ComplexMessage())))
            .Run();

        Assert.That(context.ComplexMessageReceived, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool ComplexMessageReceived;
    }

    public class EndpointUsingRegistry : EndpointConfigurationBuilder
    {
        public EndpointUsingRegistry() => EndpointSetup<NonScanningServer>(config =>
        {
            config.Handlers.NServiceBusAcceptanceTestsWithAnalyzersAssembly.AcceptanceTests.Handlers.AddComplexMessageHandler();
        });

        [Handler]
        public class ComplexMessageHandler(Context testContext) : IHandleMessages<ComplexMessage>
        {
            public Task Handle(ComplexMessage message, IMessageHandlerContext context)
            {
                testContext.ComplexMessageReceived = true;
                testContext.MarkAsCompleted();
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