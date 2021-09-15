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
            Requires.MessageDrivenPubSub();

            var ctx = await Scenario.Define<CatchAllEventHandlerScenarioContext>()
                .WithEndpoint<EndpointWithCatchAllHandler>(b
                    => b.When(session => session.Subscribe<SomeEvent>())
                        .When(
                        c => c.EndpointSubscribed,
                        session => session.Publish(new SomeEvent())
                    )
                )
                .Done(x => x.CatchAllHandlerWasCalled)
                .Run();


            Assert.IsTrue(ctx.CatchAllHandlerWasCalled);
        }

        class EndpointWithCatchAllHandler : EndpointConfigurationBuilder
        {
            public EndpointWithCatchAllHandler()
            {
                EndpointSetup<DefaultServer>(
                    config => config.OnEndpointSubscribed<CatchAllEventHandlerScenarioContext>(
                            (sub, ctx) => ctx.EndpointSubscribed = true
                        ),
                    metadata => metadata.RegisterPublisherFor<SomeEvent>(typeof(EndpointWithCatchAllHandler))
                );
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
            public bool EndpointSubscribed { get; set; }
            public bool CatchAllHandlerWasCalled { get; set; }
        }
    }
}