namespace NServiceBus.AcceptanceTests.Core.Installers;

using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using FakeTransport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

public class When_installing_endpoint_with_user_hosted_services : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_start_user_services_and_gracefully_shut_down()
    {
        var fakeTransport = new FakeTransport();
        var endpointConfiguration = InstallerTestHelpers.CreateEndpointConfiguration(fakeTransport, "EndpointWithUserHostedServices");

        var context = await Scenario.Define<Context>(endpointConfiguration.RegisterScenarioContext)
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

        fakeTransport.AssertDidNotStartReceivers();
    }

    class Context : InstallerTestContext
    {
        public bool UserServiceBeforeCoordinatorStartAsyncCalled { get; set; }
        public bool UserServiceAfterCoordinatorStartAsyncCalled { get; set; }
        public bool UserServiceBeforeCoordinatorStartedAsyncStoppingWasCancelled { get; set; }
        public bool UserServiceAfterCoordinatorStartedAsyncStoppingWasCancelled { get; set; }

        public new void MaybeCompleted() => MarkAsCompleted(InstallerCalled, FeatureSetupCalled, UserServiceBeforeCoordinatorStartAsyncCalled, UserServiceAfterCoordinatorStartAsyncCalled);
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