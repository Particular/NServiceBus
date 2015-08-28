namespace NServiceBus.AcceptanceTests.BestPractices
{
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_bestpractices_is_disabled : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_allow_subscribing_to_commands()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.Given((bus, c) => bus.Subscribe<MyCommand>()))
                    .Done(c => c.EndpointsStarted)
                    .Run();
        }

        [Test]
        public void Should_allow_unsubscribing_to_commands()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.Given((bus, c) => bus.Unsubscribe<MyCommand>()))
                    .Done(c => c.EndpointsStarted)
                    .Run();
        }

        [Test]
        public void Should_allow_publishing_commands()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.Given((bus, c) => bus.Publish(new MyCommand())))
                    .Done(c => c.EndpointsStarted)
                    .Run();
        }

        [Test]
        public void Should_allow_sending_events()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.Given((bus, c) => bus.Send(new MyEvent())))
                    .Done(c => c.EndpointsStarted)
                    .Run();
        }

        public class Context : ScenarioContext
        {
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c => c.DisableFeature<BestPracticeEnforcement>())
                    .AddMapping<MyCommand>(typeof(Endpoint))
                    .AddMapping<MyEvent>(typeof(Endpoint));
            }

            public class Handler : IHandleMessages<MyEvent>
            {
                public void Handle(MyEvent message)
                {
                }
            }
        }
        public class MyCommand : ICommand { }
        public class MyEvent : IEvent { }
    }
}
