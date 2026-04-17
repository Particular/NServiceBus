namespace NServiceBus.AcceptanceTests.Core.Installers;

using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using FakeTransport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

public class When_installing_endpoint_with_continue_shutdown_behavior : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_run_installers_without_stopping_host()
    {
        var fakeTransport = new FakeTransport();
        var endpointConfiguration = InstallerTestHelpers.CreateEndpointConfiguration(fakeTransport, "EndpointWithInstallerContinue");

        var context = await Scenario.Define<Context>(endpointConfiguration.RegisterScenarioContext)
            .WithServices(services =>
            {
                services.AddSingleton<IHostApplicationLifetime, FakeHostApplicationLifetime>();
                services.AddNServiceBusEndpoint(endpointConfiguration);
                services.AddNServiceBusInstallers(options => options.ShutdownBehavior = InstallersShutdownBehavior.Continue);
            })
            .WithServiceResolve(static async (provider, context, token) =>
            {
                await provider.RunHostedServices(token);
                context.HostShutdownTriggered = ((FakeHostApplicationLifetime)provider.GetRequiredService<IHostApplicationLifetime>()).StopApplicationCalled;
            })
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.InstallerCalled, Is.True, "Should run installers");
            Assert.That(context.FeatureSetupCalled, Is.True, "Should initialize Features");
            Assert.That(context.HostShutdownTriggered, Is.False, "Should not trigger host shutdown");
        }

        fakeTransport.AssertDidNotStartReceivers();
    }

    class Context : InstallerTestContext
    {
        public bool HostShutdownTriggered { get; set; }
    }
}