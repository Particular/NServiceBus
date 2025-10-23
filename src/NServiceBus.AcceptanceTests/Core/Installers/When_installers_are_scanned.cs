namespace NServiceBus.AcceptanceTests.Core.Installers;

using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Installation;
using NUnit.Framework;

public class When_installers_are_scanned : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_run_installers()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithInstaller>()
            .Done(c => c.EndpointsStarted)
            .Run();

        Assert.That(context.InstallerCalled, Is.True);
    }

    class Context : ScenarioContext
    {
        public bool InstallerCalled { get; set; }
    }

    class EndpointWithInstaller : EndpointConfigurationBuilder
    {
        public EndpointWithInstaller() =>
            // installers are enabled by default by but this makes it more clear that they need to be on
            EndpointSetup<DefaultServer>(c => c.EnableInstallers())
                .IncludeType<ScannedInstaller>(); //simulate that the installer is included in scanning

        class ScannedInstaller(Context testContext) : INeedToInstallSomething
        {
            public Task Install(string identity, CancellationToken cancellationToken = default)
            {
                testContext.InstallerCalled = true;
                return Task.CompletedTask;
            }
        }
    }
}