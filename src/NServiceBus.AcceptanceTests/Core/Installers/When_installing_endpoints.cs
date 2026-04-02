namespace NServiceBus.AcceptanceTests.Core.Installers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using Configuration.AdvancedExtensibility;
using FakeTransport;
using Features;
using Installation;
using NUnit.Framework;
using Settings;
using Transport;

public class When_installing_endpoints : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_only_execute_setup_and_complete()
    {
        var transport1 = new FakeTransport();
        var endpointConfiguration1 = CreateEndpointConfiguration(transport1, "EndpointWithInstaller1");

        var transport2 = new FakeTransport();
        EndpointConfiguration endpointConfiguration2 = CreateEndpointConfiguration(transport2, "EndpointWithInstaller2");

        var context = await Scenario.Define<Context>(ctx =>
            {
                endpointConfiguration1.GetSettings().Set(ctx);
                endpointConfiguration2.GetSettings().Set(ctx);
            })
            .WithServices(services =>
            {
                services.AddNServiceBusEndpointInstaller(endpointConfiguration1, "EndpointWithInstaller1");
                services.AddNServiceBusEndpointInstaller(endpointConfiguration2, "EndpointWithInstaller2");
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
            Assert.That(new[]
            {
                $"{nameof(TransportDefinition)}.{nameof(TransportDefinition.Initialize)}", $"{nameof(IMessageReceiver)}.{nameof(IMessageReceiver.Initialize)} for receiver Main",
            }, Is.EqualTo(transport1.StartupSequence).AsCollection, "Should not start the receivers");
            Assert.That(new[]
            {
                $"{nameof(TransportDefinition)}.{nameof(TransportDefinition.Initialize)}", $"{nameof(IMessageReceiver)}.{nameof(IMessageReceiver.Initialize)} for receiver Main",
            }, Is.EqualTo(transport2.StartupSequence).AsCollection, "Should not start the receivers");
        }
    }

    static EndpointConfiguration CreateEndpointConfiguration(FakeTransport transport, string endpointName)
    {
        var endpointConfiguration = new EndpointConfiguration(endpointName);
        endpointConfiguration.AssemblyScanner().Disable = true;
        endpointConfiguration.UsePersistence<AcceptanceTestingPersistence>();
        endpointConfiguration.UseTransport(transport);
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.EnableFeature<CustomFeature>();
        return endpointConfiguration;
    }

    class Context : ScenarioContext
    {
        public Dictionary<string, bool> InstallerCalled { get; set; } = [];
        public Dictionary<string, bool> FeatureSetupCalled { get; set; } = [];

        public void MaybeCompleted() => MarkAsCompleted(InstallerCalled.Count == 2, FeatureSetupCalled.Count == 2);
    }

    class CustomInstaller(Context testContext, IReadOnlySettings settings) : INeedToInstallSomething
    {
        public Task Install(string identity, CancellationToken cancellationToken = default)
        {
            testContext.InstallerCalled[settings.EndpointName()] = true;
            testContext.MaybeCompleted();
            return Task.CompletedTask;
        }
    }

    class CustomFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.AddInstaller<CustomInstaller>();

            var settings = context.Settings;

            var testContext = settings.Get<Context>();
            testContext.FeatureSetupCalled[settings.EndpointName()] = true;

            context.RegisterStartupTask(new CustomFeatureStartupTask(testContext));
        }

        class CustomFeatureStartupTask(Context testContext) : FeatureStartupTask
        {
            protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
            {
                testContext.MarkAsFailed(new InvalidOperationException("FeatureStartupTask should not be called"));
                return Task.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}