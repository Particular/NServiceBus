﻿namespace NServiceBus.AcceptanceTests.Config
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Settings;
    using NUnit.Framework;

    public class When__startup_is_complete : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Configure_and_setting_should_be_available_via_DI()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<StartedEndpoint>()
                    .Done(c => c.IsDone)
                    .Run();

            Assert.True(context.ConfigureIsAvailable,"Configure should be available in DI");
            Assert.True(context.SettingIsAvailable, "Setting should be available in DI");
        }

        public class Context : ScenarioContext
        {
            public bool IsDone { get; set; }
            public bool ConfigureIsAvailable { get; set; }
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

                public Configure Configure { get; set; }

                public ReadOnlySettings Settings { get; set; }


                public void Start()
                {
                    Context.ConfigureIsAvailable = Configure != null;

                    Context.SettingIsAvailable = Settings != null;

                    Context.IsDone = true;
                }

                public Task StopAsync()
                {
                    return Task.FromResult(0);
                }
            }
        }
    }


}