namespace NServiceBus.AcceptanceTests.Core.DependencyInjection
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_using_external_container : NServiceBusAcceptanceTest
    {
        static MyComponent myComponent = new MyComponent
        {
            Message = "Hello World!"
        };

        [Test]
        public async Task Make_sure_it_works()
        {
            var result = await Scenario.Define<Context>()
            .WithEndpoint<ExternalContainerEndpoint>(b => b.When(e => e.SendLocal(new SomeMessage())))

            .Done(c => c.Message != null)
            .Run();

            Assert.AreEqual(result.Message, myComponent.Message);
        }

        class Context : ScenarioContext
        {
            public string Message { get; set; }
        }

        public class ExternalContainerEndpoint : EndpointConfigurationBuilder
        {
            public ExternalContainerEndpoint()
            {
                var container = new AcceptanceTestingContainer();

                container.RegisterSingleton(typeof(MyComponent), myComponent);

                EndpointSetup<ExternalContainerServer>()
                    .ExternalContainer(container);
            }

            class SomeMessageHandler : IHandleMessages<SomeMessage>
            {
                public Context TestContext { get; set; }
                public MyComponent MyComponent { get; set; }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    TestContext.Message = MyComponent.Message;

                    return Task.FromResult(0);
                }
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