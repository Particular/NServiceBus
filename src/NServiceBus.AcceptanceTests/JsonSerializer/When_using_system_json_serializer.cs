namespace NServiceBus.AcceptanceTests.Core.JsonSerializer
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.EndpointTemplates;
    using NUnit.Framework;

    public class When_using_system_json_serializer : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_work()
        {
            var context = await Scenario.Define<Context>()
               .WithEndpoint<Endpoint>(c => c
                   .When(b => b.SendLocal(new MyMessage())))
               .Done(c => c.GotTheMessage)
               .Run();

            Assert.True(context.GotTheMessage);
        }

        public class Context : ScenarioContext
        {
            public bool GotTheMessage { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.UseSerialization<SystemJsonSerializer>();
                });
            }

            class MyHandler : IHandleMessages<MyMessage>
            {
                Context testContext;

                public MyHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    testContext.GotTheMessage = true;
                    return Task.CompletedTask;
                }
            }
        }

        public class MyMessage
        {
        }
    }
}