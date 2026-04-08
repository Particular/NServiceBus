namespace NServiceBus.AcceptanceTests.Core.Installers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Configuration.AdvancedExtensibility;
using FakeTransport;
using Features;
using Installation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Transport;

public class When_installing_endpoint : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_only_execute_setup_and_complete()
    {
        var fakeTransport = new FakeTransport();
        var endpointConfiguration = new EndpointConfiguration("EndpointWithInstaller");
        endpointConfiguration.AssemblyScanner().Disable = true;
        endpointConfiguration.UsePersistence<AcceptanceTestingPersistence>();
        endpointConfiguration.UseTransport(fakeTransport);
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();

        endpointConfiguration.EnableFeature<CustomFeature>();

        var context = new Context();
        endpointConfiguration.GetSettings().Set(context);

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(context);
        builder.Services.AddNServiceBusEndpoint(endpointConfiguration);

        await builder.InstallNServiceBusEndpoints();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.InstallerCalled, Is.True, "Should run installers");
            Assert.That(context.FeatureSetupCalled, Is.True, "Should initialize Features");
        }

        Assert.That(new[]
        {
            $"{nameof(TransportDefinition)}.{nameof(TransportDefinition.Initialize)}", $"{nameof(IMessageReceiver)}.{nameof(IMessageReceiver.Initialize)} for receiver Main",
        }, Is.EqualTo(fakeTransport.StartupSequence).AsCollection, "Should not start the receivers");
    }

    class Context
    {
        public bool InstallerCalled { get; set; }
        public bool FeatureSetupCalled { get; set; }
    }

    class CustomInstaller(Context testContext) : INeedToInstallSomething
    {
        public Task Install(string identity, CancellationToken cancellationToken = default)
        {
            testContext.InstallerCalled = true;
            return Task.CompletedTask;
        }
    }

    class CustomFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.AddInstaller<CustomInstaller>();

            var testContext = context.Settings.Get<Context>();
            testContext.FeatureSetupCalled = true;

            context.RegisterStartupTask(new CustomFeatureStartupTask(testContext));
        }

        class CustomFeatureStartupTask(Context testContext) : FeatureStartupTask
        {
            protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("FeatureStartupTask should not be called");

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }

    class UserHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException("User hosted service should not be called");

        public Task StopAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException("User hosted service should not be called");
    }
}