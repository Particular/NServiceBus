namespace NServiceBus.AcceptanceTests.BestPractices
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_bestpractices_is_disabled : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_allow_all_ops()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given(async (bus, c) =>
                    {
                        bus.Subscribe<MyCommand>();
                        bus.Unsubscribe<MyCommand>();
                        await bus.Send(new MyEvent());
                        await bus.Publish(new MyCommand());
                    }))
                    .Done(c => c.EndpointsStarted)
                    .Run();

        }

        public class Context : ScenarioContext
        {
            public bool GotTheException { get; set; }
            public Exception Exception { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c => c.DisableFeature<BestPracticeEnforcement>())
                    .AddMapping<MyCommand>(typeof(Endpoint))
                     .AddMapping<MyEvent>(typeof(Endpoint));
            }
        }
        public class MyCommand : ICommand { }
        public class MyEvent : IEvent { }
    }
}
