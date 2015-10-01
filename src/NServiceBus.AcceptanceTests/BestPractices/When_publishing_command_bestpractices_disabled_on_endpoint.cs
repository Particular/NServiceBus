namespace NServiceBus.AcceptanceTests.BestPractices
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_publishing_command_bestpractices_disabled_on_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_allow_publishing_commands()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When((bus, c) => bus.PublishAsync(new MyCommand())))
                .Done(c => c.EndpointsStarted)
                .Run();
        }

        [Test]
        public async Task Should_allow_sending_events()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When((bus, c) => bus.SendAsync(new MyEvent())))
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
                public Task Handle(MyEvent message)
                {
                    return Task.FromResult(0);
                }
            }
        }
        public class MyCommand : ICommand { }
        public class MyEvent : IEvent { }
    }
}