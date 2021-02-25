namespace NServiceBus.AcceptanceTests.Core.Installers
{
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using Installation;
    using NUnit.Framework;

    public class When_installers_not_enabled : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_run_installers()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithInstaller>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsFalse(context.InstallerCalled);
        }

        class Context : ScenarioContext
        {
            public bool InstallerCalled { get; set; }
        }

        class EndpointWithInstaller : EndpointConfigurationBuilder
        {
            public EndpointWithInstaller()
            {
                // disable installers as installers are enabled by default in DefaultServer
                EndpointSetup<DefaultServer>(c => c.GetSettings().Set("Installers.Enable", false));
            }

            public class CustomInstaller : INeedToInstallSomething
            {
                Context testContext;

                public CustomInstaller(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Install(string identity, CancellationToken cancellationToken = default)
                {
                    testContext.InstallerCalled = true;
                    return Task.FromResult(0);
                }
            }
        }
    }
}