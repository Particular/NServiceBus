namespace NServiceBus.AcceptanceTests.Core.Hosting;

using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_writing_startup_diagnostics : NServiceBusAcceptanceTest
{
    [Theory]
    [TestCase(true)]
    [TestCase(false)]
    public async Task Should_add_to_log(bool disable)
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<MyEndpoint>(b => b.CustomConfig(c =>
            {
                if (disable)
                {
                    c.DisableWritingDiagnosticsToLog();
                }
            }))
            .Done(c => c.EndpointsStarted)
            .Run();

        Assert.That(context.Logs.Any(l => l.Message.Contains("Startup diagnostics:")), Is.EqualTo(!disable));
    }

    class Context : ScenarioContext;

    class MyEndpoint : EndpointConfigurationBuilder
    {
        public MyEndpoint() =>
            EndpointSetup<DefaultServer>(c => c.CustomDiagnosticsWriter((_, _) => Task.CompletedTask))
                .EnableStartupDiagnostics();
    }
}