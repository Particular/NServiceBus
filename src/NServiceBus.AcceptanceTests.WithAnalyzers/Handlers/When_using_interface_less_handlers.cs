namespace NServiceBus.AcceptanceTests.Handlers;

using System;
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
            .Done(c => c is { WithCtorDependencyReceived: true, WithMethodDependencyReceived: true, WithCtorAndMethodDependencyReceived: true })
            .Run();

        Assert.That(context.WithCtorDependencyReceived, Is.True);
        Assert.That(context.WithMethodDependencyReceived, Is.True);
        Assert.That(context.WithCtorAndMethodDependencyReceived, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool WithCtorDependencyReceived;
        public bool WithMethodDependencyReceived;
        public bool WithCtorAndMethodDependencyReceived;
    }

    public class EndpointUsingInterfaceLessHandlers : EndpointConfigurationBuilder
    {
        public EndpointUsingInterfaceLessHandlers() => EndpointSetup<NonScanningServer>(config =>
        {
            var testingAssembly = config.Handlers.NServiceBusAcceptanceTestsWithAnalyzersAssembly;
            testingAssembly.AcceptanceTests.Handlers.AddInterfaceLessHandlerWithCtorDependency();
            testingAssembly.AcceptanceTests.Handlers.AddInterfaceLessHandlerWithMethodDependency();
            testingAssembly.AcceptanceTests.Handlers.AddInterfaceLessHandlerWithCtorAndMethodDependency();
        });

        [Handler]
        public class InterfaceLessHandlerWithCtorDependency(Context testContext)
        {
            public Task Handle(ComplexMessage message, IMessageHandlerContext context)
            {
                testContext.WithCtorDependencyReceived = true;
                return Task.CompletedTask;
            }
        }

        [Handler]
        public class InterfaceLessHandlerWithMethodDependency
        {
            public static Task Handle(ComplexMessage message, IMessageHandlerContext context, Context testContext)
            {
                testContext.WithMethodDependencyReceived = true;
                return Task.CompletedTask;
            }
        }

        [Handler]
#pragma warning disable CS9113 // Parameter is unread.
        public class InterfaceLessHandlerWithCtorAndMethodDependency(IServiceProvider provider)
#pragma warning restore CS9113 // Parameter is unread.
        {
#pragma warning disable CA1822
            public Task Handle(ComplexMessage message, IMessageHandlerContext context, Context testContext)
#pragma warning restore CA1822
            {
                testContext.WithCtorAndMethodDependencyReceived = true;
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