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

public class When_installing_endpoint_with_user_hosted_services : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_start_user_services_and_gracefully_shut_down()
    {
        var fakeTransport = new FakeTransport();
        var endpointConfiguration = new EndpointConfiguration("EndpointWithUserHostedServices");
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
                // User hosted service registered BEFORE the installer coordinator
                services.AddHostedService<UserServiceBeforeCoordinator>();
                services.AddNServiceBusEndpoint(endpointConfiguration);
                services.AddNServiceBusInstallers();
                // User hosted service registered AFTER the installer coordinator
                services.AddHostedService<UserServiceAfterCoordinator>();
            })
            .WithServiceResolve(static async (provider, token) => await provider.RunHostedServices(token))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.InstallerCalled, Is.True, "Should run installers");
            Assert.That(context.FeatureSetupCalled, Is.True, "Should initialize features");
            Assert.That(context.UserServiceBeforeCoordinatorStartAsyncCalled, Is.True, "Should call StartAsync on user service registered before coordinator");
            Assert.That(context.UserServiceAfterCoordinatorStartAsyncCalled, Is.True, "Should call StartAsync on user service registered after coordinator");
            Assert.That(context.UserServiceBeforeCoordinatorStartedAsyncStoppingWasCancelled, Is.False, "Service before coordinator should see ApplicationStopping as not cancelled in StartedAsync");
            Assert.That(context.UserServiceAfterCoordinatorStartedAsyncStoppingWasCancelled, Is.True, "Service after coordinator should see ApplicationStopping as cancelled in StartedAsync");
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
        public bool UserServiceBeforeCoordinatorStartAsyncCalled { get; set; }
        public bool UserServiceAfterCoordinatorStartAsyncCalled { get; set; }
        public bool UserServiceBeforeCoordinatorStartedAsyncStoppingWasCancelled { get; set; }
        public bool UserServiceAfterCoordinatorStartedAsyncStoppingWasCancelled { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(InstallerCalled, FeatureSetupCalled, UserServiceBeforeCoordinatorStartAsyncCalled, UserServiceAfterCoordinatorStartAsyncCalled);
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

    class UserServiceBeforeCoordinator(IHostApplicationLifetime lifetime, Context testContext) : IHostedLifecycleService
    {
        public Task StartingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            testContext.UserServiceBeforeCoordinatorStartAsyncCalled = true;
            return Task.CompletedTask;
        }

        public Task StartedAsync(CancellationToken cancellationToken = default)
        {
            testContext.UserServiceBeforeCoordinatorStartedAsyncStoppingWasCancelled = lifetime.ApplicationStopping.IsCancellationRequested;
            testContext.MaybeCompleted();
            return Task.CompletedTask;
        }

        public Task StoppingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StoppedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    class UserServiceAfterCoordinator(IHostApplicationLifetime lifetime, Context testContext) : IHostedLifecycleService
    {
        public Task StartingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            testContext.UserServiceAfterCoordinatorStartAsyncCalled = true;
            return Task.CompletedTask;
        }

        public Task StartedAsync(CancellationToken cancellationToken = default)
        {
            testContext.UserServiceAfterCoordinatorStartedAsyncStoppingWasCancelled = lifetime.ApplicationStopping.IsCancellationRequested;
            testContext.MaybeCompleted();
            return Task.CompletedTask;
        }

        public Task StoppingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StoppedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}