namespace NServiceBus.AcceptanceTests.Installers
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Installation;
    using NUnit.Framework;

    public class When_installers_disabled : NServiceBusAcceptanceTest
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
                EndpointSetup<DefaultServer>(c => c.DisableInstallers());
            }

            public class CustomInstaller : INeedToInstallSomething
            {
                Context testContext;

                public CustomInstaller(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Install(string identity)
                {
                    testContext.InstallerCalled = true;
                    return Task.FromResult(0);
                }
            }
        }
    }
}