namespace NServiceBus.AcceptanceTests.Handlers;

using System.Threading.Tasks;
using EndpointTemplates;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class When_using_interface_less_handlers : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_work()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointUsingInterfaceLessHandlers>(b =>
                b.When(async (session, _) => await session.SendLocal(new ComplexMessage())))
            .Done(c => c is { WithCtorDependencyReceived: true, WithMethodDependencyReceived: true })
            .Run();

        Assert.That(context.WithCtorDependencyReceived, Is.True);
        Assert.That(context.WithMethodDependencyReceived, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool WithCtorDependencyReceived;
        public bool WithMethodDependencyReceived;
    }

    public class EndpointUsingInterfaceLessHandlers : EndpointConfigurationBuilder
    {
        public EndpointUsingInterfaceLessHandlers() => EndpointSetup<NonScanningServer>(config =>
        {
            config.AddEndpointUsingInterfaceLessHandlersHandlers();
        });

        [Handler("EndpointUsingInterfaceLessHandlers")]
        public class InterfaceLessHandlerWithCtorDependency(Context testContext)
        {
            public Task Handle(ComplexMessage message, IMessageHandlerContext context)
            {
                testContext.WithCtorDependencyReceived = true;
                return Task.CompletedTask;
            }
        }

        [Handler("EndpointUsingInterfaceLessHandlers")]
        public class InterfaceLessHandlerWithMethodDependency
        {
            public static Task Handle(ComplexMessage message, IMessageHandlerContext context, Context testContext)
            {
                testContext.WithMethodDependencyReceived = true;
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