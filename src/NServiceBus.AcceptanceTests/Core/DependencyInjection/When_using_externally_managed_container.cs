namespace NServiceBus.AcceptanceTests.Core.DependencyInjection
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;

    public class When_using_externally_managed_container : NServiceBusAcceptanceTest
    {
        static MyComponent myComponent = new MyComponent();

        [Test]
        public async Task Should_use_it_for_component_resolution()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(typeof(MyComponent), myComponent);

            var result = await Scenario.Define<Context>()
            .WithEndpoint<ExternallyManagedContainerEndpoint>(b =>
            {
                IStartableEndpointWithExternallyManagedContainer configuredEndpoint = null;

                b.ToCreateInstance(
                        config =>
                        {
                            configuredEndpoint = EndpointWithExternallyManagedContainer.Create(config, serviceCollection);
                            return Task.FromResult(configuredEndpoint);
                        },
                        configured => configured.Start(serviceCollection.BuildServiceProvider())
                    )
                    .When((e, c) =>
                    {
                        //use the session provided by configure to make sure its properly populated
                        return configuredEndpoint.MessageSession.Value.SendLocal(new SomeMessage());
                    });
            })
            .Done(c => c.MessageReceived)
            .Run();

            Assert.NotNull(result.ServiceProvider, "IServiceProvider should be injectable");
            Assert.AreSame(myComponent, result.CustomService, "Should inject custom services");
        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
            public IServiceProvider ServiceProvider { get; set; }
            public MyComponent CustomService { get; set; }
        }

        public class ExternallyManagedContainerEndpoint : EndpointConfigurationBuilder
        {
            public ExternallyManagedContainerEndpoint()
            {
                EndpointSetup<ExternallyManagedContainerServer>();
            }

            class SomeMessageHandler : IHandleMessages<SomeMessage>
            {
                public SomeMessageHandler(Context context, MyComponent component, IServiceProvider serviceProvider)
                {
                    testContext = context;
                    myComponent = component;

                    testContext.CustomService = component;
                    testContext.ServiceProvider = serviceProvider;
                }

                public Task Handle(SomeMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.MessageReceived = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MyComponent
        {
        }

        public class SomeMessage : IMessage
        {
        }
    }
}