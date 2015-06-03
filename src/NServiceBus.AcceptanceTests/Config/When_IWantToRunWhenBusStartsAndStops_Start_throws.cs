namespace NServiceBus.AcceptanceTests.Config
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_Start_throws : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_shutdown_bus_cleanly()
        {
            Exception ex = null;

            Scenario.Define<Context>()
                    .WithEndpoint<StartedEndpoint>(b => b.CustomConfig(c => c.DefineCriticalErrorAction((s, e) => ex = e)))
                    .AllowExceptions()
                    .Done(c => ex != null)
                    .Run();

            Assert.IsInstanceOf<AggregateException>(ex);
            var inner1 = ex.InnerException;
            Assert.IsInstanceOf<AggregateException>(inner1);
            var inner2 = inner1.InnerException;
            Assert.AreEqual("Boom!", inner2.Message);
        }

        public class Context : ScenarioContext
        {
        }

        public class StartedEndpoint : EndpointConfigurationBuilder
        {
            public StartedEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class AfterConfigIsComplete:IWantToRunWhenBusStartsAndStops
            {
                public void Start()
                {
                    throw new Exception("Boom!");
                }

                public void Stop()
                {
                }
            }
        }
    }


}