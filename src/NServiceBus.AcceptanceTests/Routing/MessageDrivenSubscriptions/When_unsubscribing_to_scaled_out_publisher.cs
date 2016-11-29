namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using Logging;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_unsubscribing_to_scaled_out_publisher : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_send_unsubscribe_message_to_each_instance()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            return Scenario.Define<Context>(c => c.SetLogLevel(LogLevel.Debug))
                .WithEndpoint<ScaledOutPublisher>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("1")))
                .WithEndpoint<ScaledOutPublisher>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("2")))
                .WithEndpoint<Unsubscriber>(b => b.When(s => s.Unsubscribe<MyEvent>()))
                .Done(c => c.PublisherReceivedUnsubscribeMessage.Count >= 2)
                .Repeat(r => r.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(c =>
                {
                    // each instance should receive an unsubscribe message
                    Assert.That(c.PublisherReceivedUnsubscribeMessage, Does.Contain("1"));
                    Assert.That(c.PublisherReceivedUnsubscribeMessage, Does.Contain("2"));
                    Assert.That(c.PublisherReceivedUnsubscribeMessage.Count, Is.EqualTo(2));
                })
                .Run();
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
                        c.RegisterEndpointInstances(new EndpointInstance(publisherName, "1"), new EndpointInstance(publisherName, "2"));
                    },
                    metadata => metadata.RegisterPublisherFor<MyEvent>(typeof(ScaledOutPublisher)));
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}