namespace NServiceBus.AcceptanceTests.Core.DependencyInjection
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using ObjectBuilder;
    using NUnit.Framework;

    public class When_using_externally_managed_container : NServiceBusAcceptanceTest
    {
        static MyComponent myComponent = new MyComponent
        {
            Message = "Hello World!"
        };

        [Test]
        public async Task Should_use_it_for_component_resolution()
        {
            var container = new AcceptanceTestingContainer();
            container.RegisterSingleton(typeof(MyComponent), myComponent);

            var result = await Scenario.Define<Context>()
            .WithEndpoint<ExternallyManagedContainerEndpoint>(b =>
            {
                IStartableEndpointWithExternallyManagedContainer configuredEndpoint = null;

                b.ToCreateInstance(
                        config =>
                        {
                            configuredEndpoint = EndpointWithExternallyManagedContainer.Create(config, new RegistrationPhaseAdapter(container));
                            return Task.FromResult(configuredEndpoint);
                        },
                        configured => configured.Start(new ResolutionPhaseAdapter(container))
                    )
                    .When((e, c) =>
                    {
                        c.BuilderWasResolvable = container.Build(typeof(IBuilder)) != null;

                        //use the session provided by configure to make sure its properly populated
                        return configuredEndpoint.MessageSession.Value.SendLocal(new SomeMessage());
                    });
            })
            .Done(c => c.Message != null)
            .Run();

            Assert.AreEqual(result.Message, myComponent.Message);
            Assert.True(result.BuilderWasResolvable, "IBuilder should be resolvable in the container");
            Assert.False(container.WasDisposed, "Externally managed containers should not be disposed");
        }

        class Context : ScenarioContext
        {
            public string Message { get; set; }
            public bool BuilderWasResolvable { get; set; }
        }

        public class ExternallyManagedContainerEndpoint : EndpointConfigurationBuilder
        {
            public ExternallyManagedContainerEndpoint()
            {
                EndpointSetup<ExternallyManagedContainerServer>();
            }

            class SomeMessageHandler : IHandleMessages<SomeMessage>
            {
                public SomeMessageHandler(Context context, MyComponent component)
                {
                    testContext = context;
                    myComponent = component;
                }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    testContext.Message = myComponent.Message;
                    return Task.FromResult(0);
                }

                Context testContext;
                MyComponent myComponent;
            }
        }

        public class MyComponent
        {
            public string Message { get; set; }
        }

        public class SomeMessage : IMessage
        {
        }
    }
}