namespace NServiceBus.AcceptanceTests.Core.Installers;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using Configuration.AdvancedExtensibility;
using FakeTransport;
using Features;
using Installation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Transport;

public class When_installing_endpoint_with_continue_shutdown_behavior : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_run_installers_without_stopping_host()
    {
        var fakeTransport = new FakeTransport();
        var endpointConfiguration = new EndpointConfiguration("EndpointWithInstallerContinue");
        endpointConfiguration.AssemblyScanner().Disable = true;
        endpointConfiguration.UsePersistence<AcceptanceTestingPersistence>();
        endpointConfiguration.UseTransport(fakeTransport);
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();

        endpointConfiguration.EnableFeature<CustomFeature>();

        var context = await Scenario.Define<Context>(ctx =>
            {
                endpointConfiguration.GetSettings().Set(ctx);
            })
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

        Assert.That(new[]
        {
            $"{nameof(TransportDefinition)}.{nameof(TransportDefinition.Initialize)}", $"{nameof(IMessageReceiver)}.{nameof(IMessageReceiver.Initialize)} for receiver Main",
        }, Is.EqualTo(fakeTransport.StartupSequence).AsCollection, "Should not start the receivers");
    }

    class Context : ScenarioContext
    {
        public bool InstallerCalled { get; set; }
        public bool FeatureSetupCalled { get; set; }
        public bool HostShutdownTriggered { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(InstallerCalled, FeatureSetupCalled);
    }

    class CustomInstaller(Context testContext) : INeedToInstallSomething
    {
        public Task Install(string identity, CancellationToken cancellationToken = default)
        {
            testContext.InstallerCalled = true;
            testContext.MaybeCompleted();
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
            {
                testContext.MarkAsFailed(new InvalidOperationException("FeatureStartupTask should not be called"));
                return Task.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}