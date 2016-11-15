namespace NServiceBus.AcceptanceTests.BestPractices
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_unsubscribing_to_command_bestpractices_disabled_on_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_allow_unsubscribing_to_commands()
        {
            return Scenario.Define<ScenarioContext>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) => session.Unsubscribe<MyCommand>()))
                .Done(c => c.EndpointsStarted)
                // This test is only relevant for message driven transports since we need to use the mappings to
                // configure the publisher. The code first API would blow up unless we turn off the checks for the entire endpoint.
                // But if we do that turning off checks per message becomes pointless since they are already off.
                // We would need a new api to turn off startup checks only for this to be testable across the board.
                .Repeat(r => r.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(c => Assert.True(c.EndpointsStarted)).Run();
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
                    .AddMapping<MyCommand>(typeof(Endpoint));
            }
        }

        public class MyCommand : ICommand
        {
        }
    }
}