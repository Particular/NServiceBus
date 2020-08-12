namespace NServiceBus.AcceptanceTests.Core.ConfigurationHooks
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using Settings;

    public class When_configuration_finalizer_checks_feature : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_report_correct_state()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithConfigurationHook>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsTrue(context.FeatureWasEnabled);
        }

        class Context : ScenarioContext
        {
            public bool FeatureWasEnabled { get; set; }
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
                    testContext.FeatureWasEnabled = settings.IsFeatureEnabled(typeof(AutoSubscribe));
                }
            }
        }
    }
}