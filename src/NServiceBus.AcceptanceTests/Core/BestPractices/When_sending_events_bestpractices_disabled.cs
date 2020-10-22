namespace NServiceBus.AcceptanceTests.Core.BestPractices
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_events_bestpractices_disabled : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_allow_sending_events()
        {
            var context = await Scenario.Define<ScenarioContext>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.DoNotEnforceBestPractices();

                    return session.Send(new MyEvent(), sendOptions);
                }))
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.True(context.EndpointsStarted);
        }


        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var routing = c.Routing();
                    routing.RouteToEndpoint(typeof(MyEvent), typeof(Endpoint));
                    routing.RouteToEndpoint(typeof(MyCommand), typeof(Endpoint));
                });
            }

            public class Handler : IHandleMessages<MyEvent>
            {
                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }

        public class MyCommand : ICommand
        {
        }

        public class MyEvent : IEvent
        {
        }
    }
}