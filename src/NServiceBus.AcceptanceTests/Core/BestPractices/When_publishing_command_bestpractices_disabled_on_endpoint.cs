namespace NServiceBus.AcceptanceTests.Core.BestPractices
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_publishing_command_bestpractices_disabled_on_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_allow_publishing_commands()
        {
            // This test is only relevant for message driven transports since we need to use the mappings to
            // configure the publisher. The code first API would blow up unless we turn off the checks for the entire endpoint.
            // But if we do that turning off checks per message becomes pointless since they are already off.
            // We would need a new api to turn off startup checks only for this to be testable across the board.
            Requires.MessageDrivenPubSub();

            var context = await Scenario.Define<ScenarioContext>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) => session.Publish(new MyCommand())))
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.True(context.EndpointsStarted);
        }


        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    var routing = c.ConfigureTransport().Routing();
                    routing.DoNotEnforceBestPractices();
                    routing.RouteToEndpoint(typeof(MyCommand), typeof(Endpoint));
                });
            }

            public class Handler : IHandleMessages<MyCommand>
            {
                public Task Handle(MyCommand message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
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