﻿namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_subscribing_to_multiple_publishers : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_subscribe_to_all_registered_publishers_of_same_type()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<Subscriber>(e => e
                    .When(s => s.Subscribe<SomeEvent>()))
                .WithEndpoint<Publisher>(e => e
                    .CustomConfig(cfg =>
                    {
                        cfg.OverrideLocalAddress("Publisher1");
                        cfg.OnEndpointSubscribed<Context>((args, ctx) => ctx.SubscribedToPublisher1 = true);
                    }))
                .WithEndpoint<Publisher>(e => e
                    .CustomConfig(cfg =>
                    {
                        cfg.OverrideLocalAddress("Publisher2");
                        cfg.OnEndpointSubscribed<Context>((args, ctx) => ctx.SubscribedToPublisher2 = true);
                    }))
                .Repeat(r => r.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(c =>
                {
                    Assert.That(c.SubscribedToPublisher1, Is.True);
                    Assert.That(c.SubscribedToPublisher2, Is.True);
                })
                .Run();
        }

        class Context : ScenarioContext
        {
            public bool SubscribedToPublisher1 { get; set; }
            public bool SubscribedToPublisher2 { get; set; }
        }

        class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.DisableFeature<AutoSubscribe>();
                    var routing = c.MessageDrivenPubSubRouting();
                    routing.RegisterPublisher(typeof(SomeEvent).Assembly, "Publisher1");
                    routing.RegisterPublisher(typeof(SomeEvent), "Publisher2");
                });
            }
        }

        class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        class SomeEvent : IEvent
        {
        }
    }
}