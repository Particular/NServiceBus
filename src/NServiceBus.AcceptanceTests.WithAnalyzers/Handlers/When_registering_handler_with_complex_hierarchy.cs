namespace NServiceBus.AcceptanceTests.Handlers;

using System;
using System.Threading.Tasks;
using EndpointTemplates;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class When_registering_handler_with_complex_hierarchy : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_handle_message([Values] RegistrationApproach approach)
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointUsingAddHandler>(b =>
            {
                b.CustomConfig(config =>
                {
                    switch (approach)
                    {
                        case RegistrationApproach.Add:
                            config.AddHandler<EndpointUsingAddHandler.ComplexMessageHandler>();
                            break;
                        case RegistrationApproach.Registry:
                            var acceptanceTestsHandlers = config.Handlers.All.AcceptanceTests.Handlers;
                            acceptanceTestsHandlers.AddWhen_registering_handler_with_complex_hierarchyEndpointUsingAddHandlerComplexMessageHandler();
                            break;
                        default:
                           throw new InvalidOperationException("Unknown approach: " + approach + "");
                    }
                });
                b.When(async (session, _) => await session.SendLocal(new ComplexMessage()));
            })
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