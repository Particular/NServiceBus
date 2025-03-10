﻿namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions;

using System.Collections.Concurrent;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using NServiceBus.Routing;
using NUnit.Framework;

public class Unsub_from_scaled_out_pubs : NServiceBusAcceptanceTest
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
        Assert.That(context.PublisherReceivedUnsubscribeMessage, Has.Count.EqualTo(2));
    }

    class Context : ScenarioContext
    {
        public ConcurrentBag<string> PublisherReceivedUnsubscribeMessage { get; } = [];
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
                    c.GetSettings().GetOrCreate<EndpointInstances>().AddOrReplaceInstances("test",
                    [
                        new EndpointInstance(publisherName, "1"),
                        new EndpointInstance(publisherName, "2")
                    ]);
                },
                metadata => metadata.RegisterPublisherFor<MyEvent, ScaledOutPublisher>());
        }
    }

    public class MyEvent : IEvent
    {
    }
}