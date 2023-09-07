namespace NServiceBus.AcceptanceTests.Core.Hosting
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_failing_write_startup_diagnostics : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_still_start_endpoint()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<MyEndpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.True(context.Logs.Any(l => l.Message.Contains("Diagnostics write failed")));
        }

        class Context : ScenarioContext
        {
        }

        class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.CustomDiagnosticsWriter((_, __) => throw new Exception("Diagnostics write failed")))
                    .EnableStartupDiagnostics();
            }
        }
    }
}
