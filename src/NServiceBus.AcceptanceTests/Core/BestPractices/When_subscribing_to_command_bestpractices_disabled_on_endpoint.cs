namespace NServiceBus.AcceptanceTests.Core.BestPractices
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_subscribing_to_command_bestpractices_disabled_on_endpoint : NServiceBusAcceptanceTest
    {
        [Ignore("not supported via code first API")]
        [Test]
        public async Task Should_allow_subscribing_to_commands()
        {
            // This test is only relevant for message driven transports since we need to use the mappings to
            // configure the publisher. The code first API would blow up unless we turn off the checks for the entire endpoint.
            // But if we do that turning off checks per message becomes pointless since they are already off.
            // We would need a new api to turn off startup checks only for this to be testable across the board.
            Requires.MessageDrivenPubSub();

            var context = await Scenario.Define<ScenarioContext>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) => session.Subscribe<MyCommand>()))
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
                }, m => m.RegisterPublisherFor<MyCommand>(typeof(Endpoint)));
            }
        }

        public class MyCommand : ICommand
        {
        }
    }
}