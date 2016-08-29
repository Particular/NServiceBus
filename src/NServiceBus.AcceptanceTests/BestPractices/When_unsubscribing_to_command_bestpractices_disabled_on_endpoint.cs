namespace NServiceBus.AcceptanceTests.BestPractices
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_unsubscribing_to_command_bestpractices_disabled_on_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_allow_unsubscribing_to_commands()
        {
            return Scenario.Define<ScenarioContext>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) => session.Unsubscribe<MyCommand>()))
                .Done(c => c.EndpointsStarted)
                .Run();
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    var routing = c.UseTransport(r.GetTransportType()).Routing();
                    routing.DoNotEnforceBestPractices();
                })
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