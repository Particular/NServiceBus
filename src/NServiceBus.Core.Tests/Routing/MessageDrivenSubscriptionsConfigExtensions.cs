namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Configuration.AdvanceExtensibility;
    using EventNamespace;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class MessageDrivenSubscriptionsConfigExtensions
    {
        string expectedExceptionMessageForWrongEndpointName = "A logical endpoint name should not contain '@', but received 'EndpointName@MyHost'. To specify an endpoint's address use the instance mapping file for MSMQ transport, or refer to the routing documentation.";

        [Test]
        public void WhenPassingTransportAddressForPublisherInsteadOfEndpointName_ShouldThrowException()
        {
            var routingSettings = new RoutingSettings<MsmqTransport>(new SettingsHolder());

            var expectedExceptionMessage = expectedExceptionMessageForWrongEndpointName;

            var exception = Assert.Throws<ArgumentException>(() => routingSettings.RegisterPublisherForType(typeof(Event), "EndpointName@MyHost"));
            Assert.AreEqual(expectedExceptionMessage, exception.Message);
        }

        [Test]
        public void WhenPassingTransportAddressForPublisherInsteadOfEndpointName_UsingAssembly_ShouldThrowException()
        {
            var routingSettings = new RoutingSettings<MsmqTransport>(new SettingsHolder());

            var expectedExceptionMessage = expectedExceptionMessageForWrongEndpointName;

            var exception = Assert.Throws<ArgumentException>(() => routingSettings.RegisterPublisherForAssembly(Assembly.GetExecutingAssembly(), "EndpointName@MyHost"));
            Assert.AreEqual(expectedExceptionMessage, exception.Message);
        }

        [Test]
        public void WhenPassingTransportAddressForPublisherInsteadOfEndpointName_UsingAssemblyAndNamespace_ShouldThrowException()
        {
            var routingSettings = new RoutingSettings<MsmqTransport>(new SettingsHolder());

            var expectedExceptionMessage = expectedExceptionMessageForWrongEndpointName;

            var exception = Assert.Throws<ArgumentException>(() => routingSettings.RegisterPublisherForAssembly(Assembly.GetExecutingAssembly(), nameof(EventNamespace), "EndpointName@MyHost"));
            Assert.AreEqual(expectedExceptionMessage, exception.Message);
        }

        [Test]
        public void WhenPassingEndpointNameForPublisher_ShouldAddRouteToUnicastTable()
        {
            var routingSettings = new RoutingSettings<MsmqTransport>(new SettingsHolder());
            routingSettings.RegisterPublisherForType(typeof(Event), "EndpointName");
            
            var publishers = routingSettings.GetSettings().Get<Publishers>();
            
            var publishersForEvent = publishers.GetPublisherFor(typeof(Event));
            Assert.AreEqual(publishersForEvent.Count(), 1);
        }

        [Test]
        public void WhenPassingEndpointNameForPublisher_UsingAssembly_ShouldAddRoutesToUnicastTable()
        {
            var routingSettings = new RoutingSettings<MsmqTransport>(new SettingsHolder());
            routingSettings.RegisterPublisherForAssembly(Assembly.GetExecutingAssembly(), "EndpointName");

            var publishers = routingSettings.GetSettings().Get<Publishers>();

            var publishersForEvent = publishers.GetPublisherFor(typeof(Event));
            var publishersForEventWithNamespace = publishers.GetPublisherFor(typeof(EventWithNamespace));
            Assert.AreEqual(publishersForEvent.Count(), 1);
            Assert.AreEqual(publishersForEventWithNamespace.Count(), 1);
        }

        [Test]
        public void WhenPassingEndpointNameForPublisher_UsingAssemblyAndNamespace_ShouldAddRouteToUnicastTable()
        {
            var routingSettings = new RoutingSettings<MsmqTransport>(new SettingsHolder());
            routingSettings.RegisterPublisherForAssembly(Assembly.GetExecutingAssembly(), nameof(EventNamespace), "EndpointName");

            var publishers = routingSettings.GetSettings().Get<Publishers>();

            var publishersForEvent = publishers.GetPublisherFor(typeof(Event));
            var publishersForEventWithNamespace = publishers.GetPublisherFor(typeof(EventWithNamespace));
            Assert.AreEqual(publishersForEvent.Count(), 0);
            Assert.AreEqual(publishersForEventWithNamespace.Count(), 1);
        }
    }
}

namespace EventNamespace
{
    class EventWithNamespace
    {
    }
}


class Event
{
}
