namespace NServiceBus.AcceptanceTests.Outbox
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.EndpointTemplates;
    using NUnit.Framework;

    public class When_outbox_enabled_for_send_only : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_fail_prerequisites_check()
        {
            string startupDiagnostics = null;
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(e => e.CustomConfig(c => c.CustomDiagnosticsWriter((d, __) => { startupDiagnostics = d; return Task.CompletedTask; })))
                .Done(c => c.EndpointsStarted)
                .Run();

            StringAssert.Contains("Outbox is only relevant for endpoints receiving messages.", startupDiagnostics);
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
                    c.SendOnly();
                }).EnableStartupDiagnostics();
            }
        }
    }
}