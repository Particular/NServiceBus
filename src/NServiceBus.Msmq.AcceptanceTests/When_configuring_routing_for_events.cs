namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting;
    using Features;
    using NUnit.Framework;

    public class When_configuring_routing_for_events : NServiceBusAcceptanceTest
    {
        static string PublisherEndpoint => Conventions.EndpointNamingConvention(typeof(Publisher));

        [Test]
        public async Task Receives_an_event_if_endpoint_name_provided()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b => b.When(c => c.Subscribed, async (session, c) =>
                {
                    await session.Publish(new MyEvent()).ConfigureAwait(false);
                }))
                .WithEndpoint<SubscriberUsingEndpointName>()
                .Done(c => c.EventReceived)
                .Run();
        }

        [Test]
        public void Throws_exception_if_physical_address_provided()
        {
            var ae = Assert.ThrowsAsync<AggregateException>(async () => 
                await Scenario.Define<Context>()
                    .WithEndpoint<Publisher>(b => b.When(c => c.Subscribed, async (session, c) =>
                    {
                        await session.Publish(new MyEvent()).ConfigureAwait(false);
                    }))
                    .WithEndpoint<SubscriberUsingTransportAddress>()
                    .Done(c => c.EventReceived)
                    .Run());

            var expected = $"Expected an endpoint name but received '{PublisherEndpoint}@localhost'.";
            var outerExc = ae.InnerExceptions.First();

            Assert.AreEqual(typeof(ArgumentException), outerExc.InnerException.GetType());
            Assert.AreEqual(expected, outerExc.InnerException.Message);
        }

        public class Context : ScenarioContext
        {
            public bool Subscribed { get; set; }

            public bool EventReceived { get; set; }
        }

        class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.DisableFeature<AutoSubscribe>();
                    c.OnEndpointSubscribed<Context>((s, context) =>
                    {
                        context.Subscribed = true;
                    });
                    c.LimitMessageProcessingConcurrencyTo(1);
                });
            }
        }

        public class SubscriberUsingEndpointName : EndpointConfigurationBuilder
        {
            public SubscriberUsingEndpointName()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var transport = c.UseTransport<MsmqTransport>();
                    transport.Routing().RegisterPublisherForType(typeof(MyEvent), PublisherEndpoint);
                });
            }

            public class MyMessageHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    Context.EventReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class SubscriberUsingTransportAddress : EndpointConfigurationBuilder
        {
            public SubscriberUsingTransportAddress()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var transport = c.UseTransport<MsmqTransport>();
                    transport.Routing().RegisterPublisherForType(typeof(MyEvent), $"{PublisherEndpoint}@localhost");
                });
            }

            public class MyMessageHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    Context.EventReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}