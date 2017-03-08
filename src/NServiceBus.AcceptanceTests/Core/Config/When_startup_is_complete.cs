namespace NServiceBus.AcceptanceTests.Config
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using Settings;

    public class When_startup_is_complete : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Settings_should_be_available_via_DI()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<StartedEndpoint>()
                .Done(c => c.IsDone)
                .Run();

            Assert.True(context.SettingIsAvailable, "Setting should be available in DI");
        }

        class Context : ScenarioContext
        {
            public bool IsDone { get; set; }
            public bool SettingIsAvailable { get; set; }
        }

        class AfterConfigIsCompleteFeature : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(b => new AfterConfigIsCompleteFeatureTask(b.Build<ReadOnlySettings>(), b.Build<Context>()));
            }

            class AfterConfigIsCompleteFeatureTask : FeatureStartupTask
            {
                public AfterConfigIsCompleteFeatureTask(ReadOnlySettings settings, Context context)
                {
                    this.settings = settings;
                    this.context = context;
                }

                protected override Task OnStart(IMessageSession session)
                {
                    context.SettingIsAvailable = settings != null;

                    context.IsDone = true;
                    return Task.FromResult(0);
                }

                protected override Task OnStop(IMessageSession session)
                {
                    return Task.FromResult(0);
                }

                Context context;
                ReadOnlySettings settings;
            }
        }

        public class StartedEndpoint : EndpointConfigurationBuilder
        {
            public StartedEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.EnableFeature<AfterConfigIsCompleteFeature>());
            }
        }
    }
}