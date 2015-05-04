namespace NServiceBus.AcceptanceTests.BestPractices
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_bestpractices_is_disabled_for_message : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_allow_all_ops()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given((bus, c) =>
                    {
                        var sendOptions = new SendOptions();

                        sendOptions.DoNotEnforceBestPractices();

                        bus.Send(new MyEvent(), sendOptions);


                        var publishOptions = new PublishOptions();

                        publishOptions.DoNotEnforceBestPractices();

                        bus.Publish(new MyCommand(), publishOptions);
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
                EndpointSetup<DefaultServer>()
                    .AddMapping<MyCommand>(typeof(Endpoint))
                     .AddMapping<MyEvent>(typeof(Endpoint));
            }
        }
        public class MyCommand : ICommand { }
        public class MyEvent : IEvent { }
    }
}
