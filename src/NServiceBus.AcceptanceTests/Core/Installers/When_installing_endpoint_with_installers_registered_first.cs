namespace NServiceBus.AcceptanceTests.Core.Installers;

using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using FakeTransport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

public class When_installing_endpoint_with_installers_registered_first : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_only_execute_setup_and_complete()
    {
        var fakeTransport = new FakeTransport();
        var endpointConfiguration = InstallerTestHelpers.CreateEndpointConfiguration(fakeTransport, "EndpointWithInstallerFirst");

        var context = await Scenario.Define<Context>(endpointConfiguration.RegisterScenarioContext)
            .WithServices(services =>
            {
                services.AddSingleton<IHostApplicationLifetime, FakeHostApplicationLifetime>();
                // AddNServiceBusInstallers called BEFORE AddNServiceBusEndpoint
                services.AddNServiceBusInstallers();
                services.AddNServiceBusEndpoint(endpointConfiguration);
            })
            .WithServiceResolve(static async (provider, token) => await provider.RunHostedServices(token))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.InstallerCalled, Is.True, "Should run installers");
            Assert.That(context.FeatureSetupCalled, Is.True, "Should initialize Features");
        }

        fakeTransport.AssertDidNotStartReceivers();
    }

    class Context : InstallerTestContext;
}