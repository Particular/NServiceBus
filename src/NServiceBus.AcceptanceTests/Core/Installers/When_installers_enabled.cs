namespace NServiceBus.AcceptanceTests.Core.Installers;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Installation;
using NUnit.Framework;

public class When_installers_enabled : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_run_installers()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithInstaller>()
            .Done(c => c.EndpointsStarted)
            .Run();

        Assert.That(context.AsyncDisposeInstallerCalled, Is.True);
        Assert.That(context.DisposeInstallerCalled, Is.True);
        Assert.That(context.AsyncDisposeCalled, Is.True);
        Assert.That(context.DisposeCalled, Is.True);
    }

    class Context : ScenarioContext
    {
        public bool AsyncDisposeInstallerCalled { get; set; }
        public bool AsyncDisposeCalled { get; set; }
        public bool DisposeCalled { get; set; }
        public bool DisposeInstallerCalled { get; set; }
    }

    class EndpointWithInstaller : EndpointConfigurationBuilder
    {
        public EndpointWithInstaller() =>
            EndpointSetup<DefaultServer>(c =>
            {
                // installers are enabled by default but this makes it more clear that they need to be on
                c.EnableInstallers();
                c.AddInstaller<CustomInstallerWithAsyncDispose>();
                c.AddInstaller<CustomInstallerWithDispose>();
            });

        class CustomInstallerWithAsyncDispose(Context testContext) : INeedToInstallSomething, IAsyncDisposable
        {
            public Task Install(string identity, CancellationToken cancellationToken = default)
            {
                testContext.AsyncDisposeInstallerCalled = true;
                return Task.CompletedTask;
            }

            public ValueTask DisposeAsync()
            {
                testContext.AsyncDisposeCalled = true;
                return ValueTask.CompletedTask;
            }
        }

        class CustomInstallerWithDispose(Context testContext) : INeedToInstallSomething, IDisposable
        {
            public Task Install(string identity, CancellationToken cancellationToken = default)
            {
                testContext.DisposeInstallerCalled = true;
                return Task.CompletedTask;
            }

            public void Dispose() => testContext.DisposeCalled = true;
        }
    }
}