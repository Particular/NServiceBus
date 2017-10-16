namespace NServiceBus.AcceptanceTests.Core.Installers
{
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

            Assert.IsTrue(context.InstallerCalled);
        }

        class Context : ScenarioContext
        {
            public bool InstallerCalled { get; set; }
        }

        class EndpointWithInstaller : EndpointConfigurationBuilder
        {
            public EndpointWithInstaller()
            {
                EndpointSetup<DefaultServer>(c => c.EnableInstallers());
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