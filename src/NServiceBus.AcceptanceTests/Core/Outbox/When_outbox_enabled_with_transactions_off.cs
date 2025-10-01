namespace NServiceBus.AcceptanceTests.Outbox;

using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
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

        // This could all be simplified if it suported JsonPath https://github.com/dotnet/runtime/issues/31068
        var diagnosticsDoc = JsonSerializer.Deserialize<JsonObject>(startupDiagnostics);
        var features = diagnosticsDoc["Features"] as JsonArray;
        var outboxFeature = features.FirstOrDefault(node => node["Name"].GetValue<string>() == "NServiceBus.Features.Outbox");

        var satisfied = outboxFeature["PrerequisiteStatus"]["IsSatisfied"].GetValue<bool>();
        var reason = (outboxFeature["PrerequisiteStatus"]["Reasons"] as JsonArray).Single().GetValue<string>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(satisfied, Is.False);
            Assert.That(reason, Is.EqualTo("Outbox isn't needed since the receive transactions have been turned off"));
        }
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