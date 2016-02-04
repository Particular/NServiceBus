namespace NServiceBus.AcceptanceTests.DelayedDelivery
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_deferring_a_message_to_the_past : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_deliver_message()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.When((bus, c) =>
                    {
                        var options = new SendOptions();

                        options.DoNotDeliverBefore(DateTime.Now.AddHours(-1));
                        options.RouteToLocalEndpointInstance();

                        return bus.Send(new MyMessage(), options);
                    }))
                    .Done(c => c.MessageReceived)
                    .Run();

            Assert.IsTrue(context.MessageReceived);
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config => config.EnableFeature<TimeoutManager>());
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    Context.MessageReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class MyMessage : IMessage
        {
        }
    }
}