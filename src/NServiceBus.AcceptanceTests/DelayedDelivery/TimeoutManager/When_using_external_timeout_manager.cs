namespace NServiceBus.AcceptanceTests.DelayedDelivery
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_using_external_timeout_manager : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_delay_delivery()
        {
            var delay = TimeSpan.FromMilliseconds(1);

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithTimeoutManager>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) =>
                {
                    var options = new SendOptions();

                    options.DelayDeliveryWith(delay);
                    options.RouteToThisEndpoint();

                    c.SentAt = DateTime.UtcNow;

                    return session.Send(new MyMessage(), options);
                }))
                .Done(c => c.WasCalled)
                .Run();

            Assert.GreaterOrEqual(context.ReceivedAt - context.SentAt, delay);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public DateTime SentAt { get; set; }
            public DateTime ReceivedAt { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                var address = Conventions.EndpointNamingConvention(typeof(EndpointWithTimeoutManager)) + ".Timeouts";

                EndpointSetup<DefaultServer>(config => config.DisableFeature<TimeoutManager>())
                    .WithConfig<UnicastBusConfig>(c =>
                    {
                        c.TimeoutManagerAddress = address;
                    });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    Context.ReceivedAt = DateTime.UtcNow;
                    Context.WasCalled = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class EndpointWithTimeoutManager : EndpointConfigurationBuilder
        {
            public EndpointWithTimeoutManager()
            {
                EndpointSetup<DefaultServer>(config => config.EnableFeature<TimeoutManager>());
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}