namespace NServiceBus.Core.Tests.Routing.MessageDrivenSubscriptions
{
    using System;
    using System.Linq;
    using System.Reflection;
    using EventNamespace;
    using MessageNameSpace;
    using NServiceBus.Features;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NUnit.Framework;
    using OtherMesagenameSpace;
    using Settings;
    using Transport;

    [TestFixture]
    public class MessageDrivenSubscriptionsConfigExtensionsTests
    {
        [Test]
        public void WhenPassingTransportAddressForPublisherInsteadOfEndpointName_ShouldThrowException()
        {
            var routingSettings = new RoutingSettings<MessageDrivenTransportDefinition>(new SettingsHolder());

            var exception = Assert.Throws<ArgumentException>(() => routingSettings.RegisterPublisher(typeof(Event), "EndpointName@MyHost"));
            Assert.AreEqual(expectedExceptionMessageForWrongEndpointName, exception.Message);
        }

        [Test]
        public void WhenPassingTransportAddressForPublisherInsteadOfEndpointName_UsingAssembly_ShouldThrowException()
        {
            var routingSettings = new RoutingSettings<MessageDrivenTransportDefinition>(new SettingsHolder());

            var exception = Assert.Throws<ArgumentException>(() => routingSettings.RegisterPublisher(Assembly.GetExecutingAssembly(), "EndpointName@MyHost"));
            Assert.AreEqual(expectedExceptionMessageForWrongEndpointName, exception.Message);
        }

        [Test]
        public void WhenPassingTransportAddressForPublisherInsteadOfEndpointName_UsingAssemblyAndNamespace_ShouldThrowException()
        {
            var routingSettings = new RoutingSettings<MessageDrivenTransportDefinition>(new SettingsHolder());

            var exception = Assert.Throws<ArgumentException>(() => routingSettings.RegisterPublisher(Assembly.GetExecutingAssembly(), nameof(EventNamespace), "EndpointName@MyHost"));
            Assert.AreEqual(expectedExceptionMessageForWrongEndpointName, exception.Message);
        }

        [Test]
        public void WhenPassingEndpointNameForPublisher_ShouldAddRouteToPublishers()
        {
            var routingSettings = new RoutingSettings<MessageDrivenTransportDefinition>(new SettingsHolder());
            routingSettings.RegisterPublisher(typeof(Event), "EndpointName");

            var publishers = ApplyPublisherRegistrations(routingSettings);

            var publishersForEvent = publishers.GetPublisherFor(typeof(Event));
            Assert.AreEqual(publishersForEvent.Count(), 1);
        }

        [Test]
        public void WhenPassingEndpointNameForPublisher_UsingAssembly_ShouldAddAllEventsToPublishers()
        {
            var routingSettings = new RoutingSettings<MessageDrivenTransportDefinition>(new SettingsHolder());
            routingSettings.RegisterPublisher(Assembly.GetExecutingAssembly(), "EndpointName");

            var publishers = ApplyPublisherRegistrations(routingSettings);

            var publishersForEvent = publishers.GetPublisherFor(typeof(Event));
            var publishersForEventWithNamespace = publishers.GetPublisherFor(typeof(EventWithNamespace));
            Assert.AreEqual(publishersForEvent.Count(), 1);
            Assert.AreEqual(publishersForEventWithNamespace.Count(), 1);
        }

        [Test]
        public void WhenPassingEndpointNameForPublisher_UsingAssemblyAndNamespace_ShouldAddEventsWithNamespaceToPublishers()
        {
            var routingSettings = new RoutingSettings<MessageDrivenTransportDefinition>(new SettingsHolder());
            routingSettings.RegisterPublisher(Assembly.GetExecutingAssembly(), nameof(EventNamespace), "EndpointName");

            var publishers = ApplyPublisherRegistrations(routingSettings);

            var publishersForEvent = publishers.GetPublisherFor(typeof(Event));
            var publishersForEventWithNamespace = publishers.GetPublisherFor(typeof(EventWithNamespace));
            Assert.AreEqual(publishersForEvent.Count(), 0);
            Assert.AreEqual(publishersForEventWithNamespace.Count(), 1);
        }

        [Test]
        public void Should_register_all_types_in_assembly_when_not_specifying_namespace()
        {
            var routingSettings = new RoutingSettings<MessageDrivenTransportDefinition>(new SettingsHolder());
            routingSettings.RegisterPublisher(Assembly.GetExecutingAssembly(), "someAddress");

            var publishers = ApplyPublisherRegistrations(routingSettings);

            var result1 = publishers.GetPublisherFor(typeof(BaseMessage));
            var result2 = publishers.GetPublisherFor(typeof(SubMessage));
            var result3 = publishers.GetPublisherFor(typeof(EventWithoutNamespace));
            var result4 = publishers.GetPublisherFor(typeof(IMessageInterface));

            Assert.AreEqual(1, result1.Count());
            Assert.AreEqual(1, result2.Count());
            Assert.AreEqual(1, result3.Count());
            Assert.AreEqual(1, result4.Count());
        }

        [Test]
        public void Should_only_register_types_in_specified_namespace()
        {
            var routingSettings = new RoutingSettings<MessageDrivenTransportDefinition>(new SettingsHolder());
            routingSettings.RegisterPublisher(Assembly.GetExecutingAssembly(), "MessageNameSpace", "someAddress");

            var publishers = ApplyPublisherRegistrations(routingSettings);

            var result1 = publishers.GetPublisherFor(typeof(BaseMessage));
            var result2 = publishers.GetPublisherFor(typeof(SubMessage));
            var result3 = publishers.GetPublisherFor(typeof(EventWithoutNamespace));
            var result4 = publishers.GetPublisherFor(typeof(IMessageInterface));

            Assert.AreEqual(1, result1.Count());
            Assert.AreEqual(1, result4.Count());
            Assert.IsEmpty(result2);
            Assert.IsEmpty(result3);
        }

        [Theory]
        [TestCase("")]
        [TestCase(null)]
        public void Should_support_empty_namespace(string eventNamespace)
        {
            var routingSettings = new RoutingSettings<MessageDrivenTransportDefinition>(new SettingsHolder());
            routingSettings.RegisterPublisher(Assembly.GetExecutingAssembly(), eventNamespace, "someAddress");

            var publishers = ApplyPublisherRegistrations(routingSettings);

            var result1 = publishers.GetPublisherFor(typeof(BaseMessage));
            var result2 = publishers.GetPublisherFor(typeof(SubMessage));
            var result3 = publishers.GetPublisherFor(typeof(EventWithoutNamespace));
            var result4 = publishers.GetPublisherFor(typeof(IMessageInterface));

            Assert.AreEqual(1, result3.Count());
            Assert.IsEmpty(result1);
            Assert.IsEmpty(result2);
            Assert.IsEmpty(result4);
        }

        static Publishers ApplyPublisherRegistrations(RoutingSettings<MessageDrivenTransportDefinition> routingSettings)
        {
            var publishers = new Publishers();
            var conventions = new Conventions();
            conventions.IsMessageTypeAction = type => true;

            var registrations = routingSettings.Settings.Get<ConfiguredPublishers>();
            foreach (var publisherRegistration in registrations)
            {
                publisherRegistration(publishers, conventions);
            }

            return publishers;
        }

        const string expectedExceptionMessageForWrongEndpointName = "A logical endpoint name should not contain '@', but received 'EndpointName@MyHost'. To specify an endpoint's address, use the instance mapping file for the MSMQ transport, or refer to the routing documentation.";
    }

    public class MessageDrivenTransportDefinition : TransportDefinition, IMessageDrivenSubscriptionTransport
    {
        public override string ExampleConnectionStringForErrorMessage { get; }

        public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            throw new NotImplementedException();
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