namespace NServiceBus.AcceptanceTests.Handlers
{
    using System.Threading.Tasks;
    using EndpointTemplates;
    using MyMessages;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;

    public class When_multiple_namespaces : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_work()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointUsingInterfaceLessHandlers>(b =>
                    b.When(async (session, _) => await session.SendLocal(new ComplexMessage())))
                .Run();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(context.ShippingHandlerReceived, Is.True);
                Assert.That(context.OrdersHandlerReceived, Is.True);
                Assert.That(context.OrdersSubDomainHandlerReceived, Is.True);
            }
        }

        public class EndpointUsingInterfaceLessHandlers : EndpointConfigurationBuilder
        {
            public EndpointUsingInterfaceLessHandlers() => EndpointSetup<NonScanningServer>(config =>
            {
                var testingAssembly = config.Handlers.NServiceBusAcceptanceTestsWithAnalyzersAssembly;
                testingAssembly.Shipping.AddAll();
                testingAssembly.Orders.AddAll();
            });
        }
    }
}

namespace MyMessages
{
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;

    public class ComplexMessage : ConcreteParent1, IInterfaceParent1;
    public class ConcreteParent1 : ConcreteParentBase;
    public class ConcreteParentBase : IMessage;
    public interface IInterfaceParent1 : IInterfaceParent1Base;
    public interface IInterfaceParent1Base : IMessage;
    public class Context : ScenarioContext
    {
        public bool ShippingHandlerReceived;
        public bool OrdersHandlerReceived;
        public bool OrdersSubDomainHandlerReceived;

        public void MaybeCompleted() => MarkAsCompleted(ShippingHandlerReceived, OrdersHandlerReceived, OrdersSubDomainHandlerReceived);
    }
}

namespace Shipping
{
    using System.Threading.Tasks;
    using MyMessages;
    using NServiceBus;

    [Handler]
    public class HandlerWithCtorDependency(Context testContext) : IHandleMessages<ComplexMessage>
    {
        public Task Handle(ComplexMessage message, IMessageHandlerContext context)
        {
            testContext.ShippingHandlerReceived = true;
            testContext.MaybeCompleted();
            return Task.CompletedTask;
        }
    }
}

namespace Orders
{
    using System.Threading.Tasks;
    using MyMessages;
    using NServiceBus;

    [Handler]
    public class HandlerWithCtorDependency(Context testContext) : IHandleMessages<ComplexMessage>
    {
        public Task Handle(ComplexMessage message, IMessageHandlerContext context)
        {
            testContext.OrdersHandlerReceived = true;
            testContext.MaybeCompleted();
            return Task.CompletedTask;
        }
    }
}

namespace Orders.MySubDomain
{
    using System.Threading.Tasks;
    using MyMessages;
    using NServiceBus;

    [Handler]
    public class HandlerWithCtorDependency(Context testContext) : IHandleMessages<ComplexMessage>
    {
        public Task Handle(ComplexMessage message, IMessageHandlerContext context)
        {
            testContext.OrdersSubDomainHandlerReceived = true;
            testContext.MaybeCompleted();
            return Task.CompletedTask;
        }
    }
}