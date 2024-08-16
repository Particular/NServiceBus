namespace NServiceBus.AcceptanceTests.Core.Installers;

using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using FakeTransport;
using Features;
using Installation;
using Configuration.AdvancedExtensibility;
using NUnit.Framework;
using Transport;

public class When_installing_endpoint : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_only_execute_setup_and_complete()
    {
        var context = await Scenario.Define<Context>()
            .WithComponent(new InstallationOnlyComponent<EndpointWithInstaller>())
            .Run();

        Assert.That(context.InstallerCalled, Is.True, "Should run installers");
        Assert.That(context.FeatureSetupCalled, Is.True, "Should initialize Features");
        Assert.That(context.FeatureStartupTaskCalled, Is.False, "Should not start FeatureStartupTasks");
        CollectionAssert.AreEqual(context.TransportStartupSequence, new string[]
        {
            $"{nameof(TransportDefinition)}.{nameof(TransportDefinition.Initialize)}",
            $"{nameof(IMessageReceiver)}.{nameof(IMessageReceiver.Initialize)} for receiver Main",
        }, "Should not start the receivers");
    }

    class Context : ScenarioContext
    {
        public bool InstallerCalled { get; set; }
        public bool FeatureSetupCalled { get; set; }
        public bool FeatureStartupTaskCalled { get; set; }
        public FakeTransport.StartUpSequence TransportStartupSequence { get; set; }
    }

    class EndpointWithInstaller : EndpointConfigurationBuilder
    {
        public EndpointWithInstaller()
        {
            EndpointSetup<DefaultServer>((c, r) =>
            {
                // Disable installers (enabled by default in DefaultServer)
                c.GetSettings().Set("Installers.Enable", true);

                c.EnableFeature<CustomFeature>();

                // Register FakeTransport to track transport seam usage during installation
                var fakeTransport = new FakeTransport();
                c.UseTransport(fakeTransport);
                ((Context)r.ScenarioContext).TransportStartupSequence = fakeTransport.StartupSequence;
            });
        }

        class CustomInstaller : INeedToInstallSomething
        {
            Context testContext;

            public CustomInstaller(Context testContext)
            {
                this.testContext = testContext;
            }

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
                var testContext = context.Settings.Get<Context>();
                testContext.FeatureSetupCalled = true;

                context.RegisterStartupTask(new CustomFeatureStartupTask(testContext));
            }

            class CustomFeatureStartupTask : FeatureStartupTask
            {
                readonly Context testContext;

                public CustomFeatureStartupTask(Context testContext)
                {
                    this.testContext = testContext;
                }

                protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                {
                    testContext.FeatureStartupTaskCalled = true;
                    return Task.CompletedTask;
                }

                protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }
        }
    }
}