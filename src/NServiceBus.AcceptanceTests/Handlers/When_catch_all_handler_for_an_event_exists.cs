namespace NServiceBus.AcceptanceTests.Handlers
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_catch_all_handler_for_an_event_exists : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task It_should_be_invoked()
        {
            var ctx = await Scenario.Define<CatchAllEventHandlerScenarioContext>()
                .WithEndpoint<EndpointWithCatchAllHandler>(b
                    => b.When(async session
                            =>
                        {
                            await session.Subscribe<SomeEvent>();
                            await session.Publish(new SomeEvent());
                        }
                    ))
                .Done(x => x.CatchAllHandlerWasCalled)
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
                CatchAllEventHandlerScenarioContext scenarioContext;

                public CatchAllMessageHandler(CatchAllEventHandlerScenarioContext scenarioContext)
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

        public class SomeEvent : IEvent { }

        class CatchAllEventHandlerScenarioContext : ScenarioContext
        {
            public bool CatchAllHandlerWasCalled { get; set; }
        }
    }
}