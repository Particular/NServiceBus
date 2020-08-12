namespace NServiceBus.AcceptanceTests.Core.ConfigurationHooks
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Settings;

    public class When_configuration_is_finalized : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_run_configuration_hook()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithConfigurationHook>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsTrue(context.HookWasCalled);
        }

        class Context : ScenarioContext
        {
            public bool HookWasCalled { get; set; }
        }

        class EndpointWithConfigurationHook : EndpointConfigurationBuilder
        {
            public EndpointWithConfigurationHook()
            {
                EndpointSetup<DefaultServer>();
            }

            public class CustomInstaller : IWantToRunBeforeConfigurationIsFinalized
            {
                public void Run(SettingsHolder settings)
                {
                    var testContext = settings.Get<Context>();
                    testContext.HookWasCalled = true;
                }
            }
        }
    }
}