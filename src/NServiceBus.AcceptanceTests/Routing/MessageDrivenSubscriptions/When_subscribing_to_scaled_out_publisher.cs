namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using Features;
    using Logging;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_subscribing_to_scaled_out_publisher : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_send_subscription_message_to_each_instance()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            return Scenario.Define<Context>(c => c.SetLogLevel(LogLevel.Debug))
                .WithEndpoint<ScaledOutPublisher>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("1")))
                .WithEndpoint<ScaledOutPublisher>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("2")))
                .WithEndpoint<Subscriber>(b => b.When(s => s.Subscribe<MyEvent>()))
                .Done(c => c.PublisherReceivedSubscription.Count >= 2)
                .Repeat(r => r.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(c =>
                {
                    // each instance should receive a subscription message
                    Assert.That(c.PublisherReceivedSubscription, Does.Contain("1"));
                    Assert.That(c.PublisherReceivedSubscription, Does.Contain("2"));
                    Assert.That(c.PublisherReceivedSubscription.Count, Is.EqualTo(2));
                })
                .Run();
        }

        class Context : ScenarioContext
        {
            public List<string> PublisherReceivedSubscription { get; } = new List<string>();
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
                    c => c.EnableFeature<EndpointInstancesConfigurationFeature>(),
                    metadata => metadata.RegisterPublisherFor<MyEvent>(typeof(ScaledOutPublisher)));
            }

            class EndpointInstancesConfigurationFeature : Feature
            {
                protected override void Setup(FeatureConfigurationContext context)
                {
                    var publisherName = Conventions.EndpointNamingConvention(typeof(ScaledOutPublisher));
                    context.Routing.EndpointInstances.AddOrReplaceInstances("testing", new List<EndpointInstance>
                    {
                        new EndpointInstance(publisherName, "1"), new EndpointInstance(publisherName, "2")
                    });
                }
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}