namespace NServiceBus.AcceptanceTests.Basic
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_registering_handlers_explicitly : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_enable_properties_to_be_set()
        {
            var simpleValue = "SomeValue";

            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b =>
                    {
                        b.CustomConfig(c => c.RegisterComponents(r => r.ConfigureComponent(builder => new Endpoint.MyMessageHandler(builder.Build<Context>())
                        {
                            MySimpleDependency = simpleValue
                        }, DependencyLifecycle.InstancePerCall)));
                        b.When((bus, c) => bus.SendLocal(new MyMessage()));
                    })
                    .Done(c => c.WasCalled)
                    .Run();

            Assert.AreEqual(simpleValue, context.PropertyValue);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public string PropertyValue { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public MyMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public string MySimpleDependency { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    testContext.PropertyValue = MySimpleDependency;
                    testContext.WasCalled = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
            public class MyPropDependency
            {
            }
        }

        public class MyMessage : ICommand
        {
        }
    }
}