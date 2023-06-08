namespace NServiceBus.AcceptanceTests.Outbox
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_outbox_enabled_with_transactions_off : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_fail_prerequisites_check()
        {
            string startupDiagnostics = null;
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(e => e.CustomConfig(c => c.CustomDiagnosticsWriter((d, __) => { startupDiagnostics = d; return Task.CompletedTask; })))
                .Done(c => c.EndpointsStarted)
                .Run();

            StringAssert.Contains("Outbox isn't needed since the receive transactions have been turned off", startupDiagnostics);
        }

        public class Context : ScenarioContext
        {
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableOutbox();
                    c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.None;
                }).EnableStartupDiagnostics();
            }
        }
    }
}