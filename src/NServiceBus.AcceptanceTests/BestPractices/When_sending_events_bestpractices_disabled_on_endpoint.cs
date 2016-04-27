namespace NServiceBus.AcceptanceTests.BestPractices
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_sending_events_bestpractices_disabled_on_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_allow_sending_events()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) => session.Send(new MyEvent())))
                .Done(c => c.EndpointsStarted)
                .Run();
        }

        public class Context : ScenarioContext
        {
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c => c.DisableFeature<BestPracticeEnforcement>())
                    .AddMapping<MyCommand>(typeof(Endpoint))
                    .AddMapping<MyEvent>(typeof(Endpoint));
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