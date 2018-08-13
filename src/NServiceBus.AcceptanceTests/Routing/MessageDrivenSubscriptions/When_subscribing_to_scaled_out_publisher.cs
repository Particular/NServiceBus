namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using NServiceBus.Routing;
    using NUnit.Framework;

    public class When_subscribing_to_scaled_out_publisher : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_send_subscription_message_to_each_instance()
        {
            Requires.MessageDrivenPubSub();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<ScaledOutPublisher>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("1")))
                .WithEndpoint<ScaledOutPublisher>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("2")))
                .WithEndpoint<Subscriber>(b => b.When(s => s.Subscribe<MyEvent>()))
                .Done(c => c.PublisherReceivedSubscription.Count >= 2)
                .Run();

            // each instance should receive a subscription message
            Assert.That(context.PublisherReceivedSubscription, Does.Contain("1"));
            Assert.That(context.PublisherReceivedSubscription, Does.Contain("2"));
            Assert.That(context.PublisherReceivedSubscription.Count, Is.EqualTo(2));
        }

        class Context : ScenarioContext
        {
            public ConcurrentBag<string> PublisherReceivedSubscription { get; } = new ConcurrentBag<string>();
        }

        class ScaledOutPublisher : EndpointConfigurationBuilder
        {
            public ScaledOutPublisher()
            {
                // store the instance discriminator of each instance receiving a subscription message:
                EndpointSetup<DefaultServer>(c => c
                    .OnEndpointSubscribed<Context>((subscription, context) =>
                        context.PublisherReceivedSubscription.Add(c.GetSettings().Get<string>("EndpointInstanceDiscriminator"))));
            }
        }

        class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(
                    c =>
                    {
                        var publisherName = Conventions.EndpointNamingConvention(typeof(ScaledOutPublisher));
                        c.GetSettings().GetOrCreate<EndpointInstances>().AddOrReplaceInstances("test", new List<EndpointInstance>
                        {
                            new EndpointInstance(publisherName, "1"),
                            new EndpointInstance(publisherName, "2")
                        });
                    },
                    metadata => metadata.RegisterPublisherFor<MyEvent>(typeof(ScaledOutPublisher)));
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}