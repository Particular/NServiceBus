namespace NServiceBus.AcceptanceTests.Config
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Settings;
    using NUnit.Framework;

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

        public class Context : ScenarioContext
        {
            public bool IsDone { get; set; }
            public bool SettingIsAvailable { get; set; }
        }

        public class StartedEndpoint : EndpointConfigurationBuilder
        {
            public StartedEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class AfterConfigIsComplete:IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public ReadOnlySettings Settings { get; set; }


                public Task Start(IBusSession session)
                {
                    Context.SettingIsAvailable = Settings != null;

                    Context.IsDone = true;
                    return Task.FromResult(0);
                }

                public Task Stop(IBusSession session)
                {
                    return Task.FromResult(0);
                }
            }
        }
    }


}