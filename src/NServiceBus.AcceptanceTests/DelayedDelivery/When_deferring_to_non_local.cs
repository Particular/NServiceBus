namespace NServiceBus.AcceptanceTests.DelayedDelivery
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_deferring_to_non_local : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Message_should_be_received()
        {
            var delay = TimeSpan.FromSeconds(2);

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) =>
                {
                    var options = new SendOptions();

                    options.DelayDeliveryWith(delay);

                    c.SentAt = DateTime.UtcNow;

                    return session.Send(new MyMessage(), options);
                }))
                .WithEndpoint<Receiver>()
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
                EndpointSetup<DefaultServer>(config =>
                {
                    config.EnableFeature<TimeoutManager>();
                    config.ConfigureTransport().Routing().RouteToEndpoint(typeof(MyMessage), typeof(Receiver));
                });
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public MyMessageHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    testContext.ReceivedAt = DateTime.UtcNow;
                    testContext.WasCalled = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MyMessage : ICommand
        {
        }
    }
}