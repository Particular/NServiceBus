namespace NServiceBus.AcceptanceTests.BestPractices
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_publishing_command_bestpractices_disabled_on_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_allow_publishing_commands()
        {
            return Scenario.Define<ScenarioContext>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) => session.Publish(new MyCommand())))
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
                    },
                    metadata => metadata.RegisterPublisherFor<MyCommand>(typeof(Endpoint)));
            }

            public class Handler : IHandleMessages<MyCommand>
            {
                public Task Handle(MyCommand message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }

        public class MyCommand : ICommand
        {
        }
    }
}