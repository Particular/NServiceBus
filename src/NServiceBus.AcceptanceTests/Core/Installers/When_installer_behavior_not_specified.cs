namespace NServiceBus.AcceptanceTests.Installers
{
    using System.Diagnostics;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using Installation;
    using NUnit.Framework;

    public class When_installer_behavior_not_specified : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_run_installers_when_no_debugger_attached()
        {
            if (Debugger.IsAttached)
            {
                Assert.Ignore("This test must run without a debugger attached but a debugger is attached.");
            }

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithInstaller>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsFalse(context.InstallerCalled);
        }

        [Test]
        public async Task Should_run_installers_when_debugger_attached()
        {
            if (!Debugger.IsAttached)
            {
                Assert.Ignore("This test must run with a debugger attached but no debugger is attached.");
            }

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
                // there is no public API to specify "no user specified behavior"
                EndpointSetup<DefaultServer>(c => c.GetSettings().Set("Installers.Enable", null));
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