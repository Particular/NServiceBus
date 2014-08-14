﻿namespace NServiceBus.AcceptanceTests.Configuration
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_IWantToRunWhenBusStartsAndStops_Start_throws : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_shutdown_bus_cleanly()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<StartedEndpoint>()
                    .Done(c => c.IsDone)
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool IsDone { get; set; }
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

                public void Start()
                {
                    Context.IsDone = true;

                    throw new Exception("Boom!");
                }

                public void Stop()
                {
                }
            }
        }
    }


}