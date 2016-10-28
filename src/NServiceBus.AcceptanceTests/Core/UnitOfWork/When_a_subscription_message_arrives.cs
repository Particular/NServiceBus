namespace NServiceBus.AcceptanceTests.Core.UnitOfWork
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.UnitOfWork;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_a_subscription_message_arrives : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_invoke_uow()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<UOWEndpoint>()
                .Done(c => c.UowWasCalled)
                .Repeat(b => b.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(c => Assert.True(c.UowWasCalled))
                .Run();
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
                public Context Context { get; set; }

                public Task Begin()
                {
                    Context.UowWasCalled = true;
                    return Task.FromResult(0);
                }

                public Task End(Exception ex = null)
                {
                    return Task.FromResult(0);
                }
            }

            public class DummyHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

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