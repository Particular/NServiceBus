namespace NServiceBus.AcceptanceTests.Core.Installers;

using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using FakeTransport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

public class When_installing_multiple_times : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_throw_when_called_twice()
    {
        var fakeTransport = new FakeTransport();
        var endpointConfiguration = InstallerTestHelpers.CreateEndpointConfiguration(fakeTransport, "EndpointWithInstaller");

        var context = await Scenario.Define<Context>(endpointConfiguration.RegisterScenarioContext)
            .WithServices(services =>
            {
                services.AddSingleton<IHostApplicationLifetime, FakeHostApplicationLifetime>();
                services.AddNServiceBusEndpoint(endpointConfiguration);
                services.AddNServiceBusInstallers(); // First call
                services.AddNServiceBusInstallers(); // Second call - should not throw
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

    [Test]
    public async Task Should_allow_setting_ShutdownBehavior_on_second_call()
    {
        var fakeTransport = new FakeTransport();
        var endpointConfiguration = InstallerTestHelpers.CreateEndpointConfiguration(fakeTransport, "EndpointWithInstaller");

        var context = await Scenario.Define<Context>(endpointConfiguration.RegisterScenarioContext)
            .WithServices(services =>
            {
                services.AddSingleton<IHostApplicationLifetime, FakeHostApplicationLifetime>();
                services.AddNServiceBusEndpoint(endpointConfiguration);
                services.AddNServiceBusInstallers(); // First call with default behavior
                services.AddNServiceBusInstallers(options => options.ShutdownBehavior = InstallersShutdownBehavior.Continue); // Second call to modify behavior
            })
            .WithServiceResolve(static async (provider, token) => await provider.RunHostedServices(token))
            .Run();

        // With Continue behavior, the application should not shut down automatically
        // We can't easily test the actual shutdown behavior in this test context,
        // but we can verify that no exception was thrown and installers ran
        Assert.That(context.InstallerCalled, Is.True, "Should run installers");
        Assert.That(context.FeatureSetupCalled, Is.True, "Should initialize Features");

        fakeTransport.AssertDidNotStartReceivers();
    }

    class Context : InstallerTestContext;
}
