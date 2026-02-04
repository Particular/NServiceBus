namespace NServiceBus.AcceptanceTests.Registrations.Handlers;

using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

public class When_registering_handler_with_complex_hierarchy : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_handle_message([Values] RegistrationApproach approach)
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointUsingAddHandler>(b =>
                b.CustomRegistrations(approach,
                        static config => config.AddHandler<EndpointUsingAddHandler.ComplexMessageHandler>(),
                        static registry => registry.Registrations.Handlers.AddWhen_registering_handler_with_complex_hierarchy__EndpointUsingAddHandler__ComplexMessageHandler())
                    .When(async (session, _) => await session.SendLocal(new ComplexMessage())))
            .Run();

        Assert.That(context.ComplexMessageReceived, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool ComplexMessageReceived;
    }

    public class EndpointUsingAddHandler : EndpointConfigurationBuilder
    {
        public EndpointUsingAddHandler() => EndpointSetup<NonScanningServer>();

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