namespace NServiceBus.AcceptanceTests.Core.DependencyInjection
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_using_external_container : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Make_sure_it_works()
        {
            var container = new AcceptanceTestingContainer();
            var myComponent = new MyComponent
            {
                Message = "Hello World!"
            };
            container.RegisterSingleton(typeof(MyComponent), myComponent);

            var result = await Scenario.Define<Context>()
                .WithEndpoint<ExternalContainerEndpoint>(b => b.CustomConfig(c => c.GetSettings().Set("ExternalContainer", container)).When(e => e.SendLocal(new SomeMessage())))
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
                EndpointSetup<ExternalContainerServer>();
            }

            class SomeMessageHandler : IHandleMessages<SomeMessage>
            {
                public Context TestContext { get; set; }
                public MyComponent MyComponent {get; set; }

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