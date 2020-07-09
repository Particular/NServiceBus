namespace NServiceBus.AcceptanceTests.Core.UnitOfWork
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.UnitOfWork;
    using NUnit.Framework;

    public class When_a_subscription_message_arrives : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_invoke_uow()
        {
            Requires.MessageDrivenPubSub();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<UOWEndpoint>()
                .Done(c => c.UowWasCalled)
                .Run();

            Assert.True(context.UowWasCalled);
        }

        public class Context : ScenarioContext
        {
            public bool UowWasCalled { get; set; }
        }

        public class UOWEndpoint : EndpointConfigurationBuilder
        {
            public UOWEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.RegisterComponents(container => container.ConfigureComponent<MyUow>(DependencyLifecycle.InstancePerCall)),
                    metadata => metadata.RegisterPublisherFor<MyMessage>(typeof(UOWEndpoint)));
            }

            class MyUow : IManageUnitsOfWork
            {
                public MyUow(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Begin()
                {
                    testContext.UowWasCalled = true;
                    return Task.FromResult(0);
                }

                public Task End(Exception ex = null)
                {
                    return Task.FromResult(0);
                }

                Context testContext;
            }

            public class DummyHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }

        public class MyMessage : IEvent
        {
        }
    }
}