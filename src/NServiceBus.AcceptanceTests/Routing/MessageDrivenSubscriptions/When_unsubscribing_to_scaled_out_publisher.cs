namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using NServiceBus.Routing;
    using NUnit.Framework;

    public class When_unsubscribing_to_scaled_out_publisher : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_send_unsubscribe_message_to_each_instance()
        {
            Requires.MessageDrivenPubSub();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<ScaledOutPublisher>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("1")))
                .WithEndpoint<ScaledOutPublisher>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("2")))
                .WithEndpoint<Unsubscriber>(b => b.When(s => s.Unsubscribe<MyEvent>()))
                .Done(c => c.PublisherReceivedUnsubscribeMessage.Count >= 2)
                .Run();

            // each instance should receive an unsubscribe message
            Assert.That(context.PublisherReceivedUnsubscribeMessage, Does.Contain("1"));
            Assert.That(context.PublisherReceivedUnsubscribeMessage, Does.Contain("2"));
            Assert.That(context.PublisherReceivedUnsubscribeMessage.Count, Is.EqualTo(2));
        }

        class Context : ScenarioContext
        {
            public List<string> PublisherReceivedUnsubscribeMessage { get; } = new List<string>();
        }

        class ScaledOutPublisher : EndpointConfigurationBuilder
        {
            public ScaledOutPublisher()
            {
                // store the instance discriminator of each instance receiving a unsubscribe message:
                EndpointSetup<DefaultServer>(c =>
                {
                    c.OnEndpointUnsubscribed<Context>((subscription, context) =>
                            context.PublisherReceivedUnsubscribeMessage.Add(c.GetSettings().Get<string>("EndpointInstanceDiscriminator")));
                });
            }
        }

        class Unsubscriber : EndpointConfigurationBuilder
        {
            public Unsubscriber()
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