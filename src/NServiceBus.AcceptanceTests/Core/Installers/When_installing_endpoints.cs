namespace NServiceBus.AcceptanceTests.Core.Installers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using FakeTransport;
using Features;
using Installation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Settings;

public class When_installing_endpoints : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_only_execute_setup_and_complete()
    {
        var transport1 = new FakeTransport();
        var endpointConfiguration1 = CreateEndpointConfiguration(transport1, "EndpointWithInstaller1");

        var transport2 = new FakeTransport();
        var endpointConfiguration2 = CreateEndpointConfiguration(transport2, "EndpointWithInstaller2");

        var context = await Scenario.Define<Context>(ctx =>
            {
                endpointConfiguration1.RegisterScenarioContext(ctx);
                endpointConfiguration2.RegisterScenarioContext(ctx);
            })
            .WithServices(services =>
            {
                services.AddSingleton<IHostApplicationLifetime, FakeHostApplicationLifetime>();
                services.AddNServiceBusEndpoint(endpointConfiguration1, "EndpointWithInstaller1");
                services.AddNServiceBusEndpoint(endpointConfiguration2, "EndpointWithInstaller2");
                services.AddNServiceBusInstallers();
            })
            .WithServiceResolve(static async (provider, token) => await provider.RunHostedServices(token))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.InstallerCalled.All(x => x.Value), Is.True, "Should run installers");
            Assert.That(context.FeatureSetupCalled.All(x => x.Value), Is.True, "Should initialize Features");
        }

        using (Assert.EnterMultipleScope())
        {
            transport1.AssertDidNotStartReceivers();
            transport2.AssertDidNotStartReceivers();
        }
    }

    static EndpointConfiguration CreateEndpointConfiguration(FakeTransport transport, string endpointName)
    {
        var endpointConfiguration = new EndpointConfiguration(endpointName);
        endpointConfiguration.AssemblyScanner().Disable = true;
        endpointConfiguration.UsePersistence<AcceptanceTestingPersistence>();
        endpointConfiguration.UseTransport(transport);
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.EnableFeature<MultiEndpointInstallerFeature>();
        return endpointConfiguration;
    }

    class Context : ScenarioContext
    {
        public Dictionary<string, bool> InstallerCalled { get; set; } = [];
        public Dictionary<string, bool> FeatureSetupCalled { get; set; } = [];

        public void MaybeCompleted() => MarkAsCompleted(InstallerCalled.Count == 2, FeatureSetupCalled.Count == 2);
    }

    class MultiEndpointInstallerFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.AddInstaller<MultiEndpointInstaller>();

            var testContext = context.Settings.Get<Context>();
            testContext.FeatureSetupCalled[context.Settings.EndpointName()] = true;

            context.RegisterStartupTask(new MultiEndpointInstallerStartupTask(testContext));
        }
    }

    class MultiEndpointInstaller(Context testContext, IReadOnlySettings settings) : INeedToInstallSomething
    {
        public Task Install(string identity, CancellationToken cancellationToken = default)
        {
            testContext.InstallerCalled[settings.EndpointName()] = true;
            testContext.MaybeCompleted();
            return Task.CompletedTask;
        }
    }

    class MultiEndpointInstallerStartupTask(Context testContext) : FeatureStartupTask
    {
        protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
        {
            testContext.MarkAsFailed(new InvalidOperationException("FeatureStartupTask should not be called"));
            return Task.CompletedTask;
        }

        protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}