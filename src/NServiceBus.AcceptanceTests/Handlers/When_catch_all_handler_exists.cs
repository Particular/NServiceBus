namespace NServiceBus.AcceptanceTests.Handlers
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_catch_all_handler_exists : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task It_should_be_invoked()
        {
            var ctx = await Scenario.Define<CatchAllHandlerScenarioContext>()
                .WithEndpoint<EndpointWithCatchAllHandler>(b
                    => b.When(session
                        => session.SendLocal(new SomeMessage())
                    )
                )
                .Done(c => c.CatchAllHandlerWasCalled)
                .Run();

            Assert.IsTrue(ctx.CatchAllHandlerWasCalled);
        }


        class EndpointWithCatchAllHandler : EndpointConfigurationBuilder
        {
            public EndpointWithCatchAllHandler()
            {
                EndpointSetup<DefaultServer>();
            }

            public class CatchAllMessageHandler : IHandleMessages<object>
            {
                CatchAllHandlerScenarioContext scenarioContext;

                public CatchAllMessageHandler(CatchAllHandlerScenarioContext scenarioContext)
                {
                    this.scenarioContext = scenarioContext;
                }

                public Task Handle(object message, IMessageHandlerContext context)
                {
                    scenarioContext.CatchAllHandlerWasCalled = true;
                    return Task.CompletedTask;
                }
            }
        }

        public class SomeMessage : IMessage { }

        class CatchAllHandlerScenarioContext : ScenarioContext
        {
            public bool CatchAllHandlerWasCalled { get; set; }
        }
    }
}
