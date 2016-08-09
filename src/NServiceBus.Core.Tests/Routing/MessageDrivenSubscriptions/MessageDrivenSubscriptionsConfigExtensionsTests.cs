﻿using NServiceBus;

namespace NServiceBus.Core.Tests.Routing.MessageDrivenSubscriptions
{
    using System;
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
    public class RoutingSettingsTests
    {
        [Test]
        public void WhenPassingTransportAddressForPublisherInsteadOfEndpointName_ShouldThrowException()
        {
            var settings = new SettingsHolder();
            settings.Set<Conventions>(new Conventions());
            var routingSettings = new RoutingSettings<MessageDrivenTransportDefinition>(settings);

            var exception = Assert.Throws<ArgumentException>(() => routingSettings.RegisterPublisher(typeof(Event), "EndpointName@MyHost"));
            Assert.AreEqual(expectedExceptionMessageForWrongEndpointName, exception.Message);
        }

        [Test]
        public void WhenPassingTransportAddressForPublisherInsteadOfEndpointName_UsingAssembly_ShouldThrowException()
        {
            var settings = new SettingsHolder();
            settings.Set<Conventions>(new Conventions());
            var routingSettings = new RoutingSettings<MessageDrivenTransportDefinition>(settings);

            var exception = Assert.Throws<ArgumentException>(() => routingSettings.RegisterPublisher(Assembly.GetExecutingAssembly(), "EndpointName@MyHost"));
            Assert.AreEqual(expectedExceptionMessageForWrongEndpointName, exception.Message);
        }

        [Test]
        public void WhenPassingTransportAddressForPublisherInsteadOfEndpointName_UsingAssemblyAndNamespace_ShouldThrowException()
        {
            var settings = new SettingsHolder();
            settings.Set<Conventions>(new Conventions());
            var routingSettings = new RoutingSettings<MessageDrivenTransportDefinition>(settings);

            var exception = Assert.Throws<ArgumentException>(() => routingSettings.RegisterPublisher(Assembly.GetExecutingAssembly(), nameof(EventNamespace), "EndpointName@MyHost"));
            Assert.AreEqual(expectedExceptionMessageForWrongEndpointName, exception.Message);
        }

        [Test]
        public void WhenPassingEndpointNameForPublisher_ShouldAddRouteToPublishers()
        {
            var settings = new SettingsHolder();
            settings.Set<Conventions>(new Conventions());
            var routingSettings = new RoutingSettings<MessageDrivenTransportDefinition>(settings);
            routingSettings.RegisterPublisher(typeof(Event), "EndpointName");

            var publishers = ApplyPublisherRegistrations(routingSettings);

            var publishersForEvent = publishers.GetPublisherFor(typeof(Event));
            Assert.IsNotNull(publishersForEvent);
        }

        [Test]
        public void WhenPassingEndpointNameForPublisher_UsingAssembly_ShouldAddAllEventsToPublishers()
        {
            var settings = new SettingsHolder();
            settings.Set<Conventions>(new Conventions());
            var routingSettings = new RoutingSettings<MessageDrivenTransportDefinition>(settings);
            routingSettings.RegisterPublisher(Assembly.GetExecutingAssembly(), "EndpointName");

            var publishers = ApplyPublisherRegistrations(routingSettings);

            var publishersForEvent = publishers.GetPublisherFor(typeof(Event));
            var publishersForEventWithNamespace = publishers.GetPublisherFor(typeof(EventWithNamespace));

            Assert.IsNotNull(publishersForEvent);
            Assert.IsNotNull(publishersForEventWithNamespace);
        }

        [Test]
        public void WhenPassingEndpointNameForPublisher_UsingAssemblyAndNamespace_ShouldAddEventsWithNamespaceToPublishers()
        {
            var settings = new SettingsHolder();
            settings.Set<Conventions>(new Conventions());
            var routingSettings = new RoutingSettings<MessageDrivenTransportDefinition>(settings);
            routingSettings.RegisterPublisher(Assembly.GetExecutingAssembly(), nameof(EventNamespace), "EndpointName");

            var publishers = ApplyPublisherRegistrations(routingSettings);

            var publishersForEvent = publishers.GetPublisherFor(typeof(Event));
            var publishersForEventWithNamespace = publishers.GetPublisherFor(typeof(EventWithNamespace));

            Assert.IsNull(publishersForEvent);
            Assert.IsNotNull(publishersForEventWithNamespace);
        }

        [Test]
        public void Should_register_all_types_in_assembly_when_not_specifying_namespace()
        {
            var settings = new SettingsHolder();
            settings.Set<Conventions>(new Conventions());
            var routingSettings = new RoutingSettings<MessageDrivenTransportDefinition>(settings);
            routingSettings.RegisterPublisher(Assembly.GetExecutingAssembly(), "someAddress");

            var publishers = ApplyPublisherRegistrations(routingSettings);

            var result1 = publishers.GetPublisherFor(typeof(BaseMessage));
            var result2 = publishers.GetPublisherFor(typeof(SubMessage));
            var result3 = publishers.GetPublisherFor(typeof(EventWithoutNamespace));
            var result4 = publishers.GetPublisherFor(typeof(IMessageInterface));

            Assert.IsNotNull(result1);
            Assert.IsNotNull(result2);
            Assert.IsNotNull(result3);
            Assert.IsNotNull(result4);
        }

        [Test]
        public void Should_only_register_types_in_specified_namespace()
        {
            var settings = new SettingsHolder();
            settings.Set<Conventions>(new Conventions());
            var routingSettings = new RoutingSettings<MessageDrivenTransportDefinition>(settings);
            routingSettings.RegisterPublisher(Assembly.GetExecutingAssembly(), "MessageNameSpace", "someAddress");

            var publishers = ApplyPublisherRegistrations(routingSettings);

            var result1 = publishers.GetPublisherFor(typeof(BaseMessage));
            var result2 = publishers.GetPublisherFor(typeof(SubMessage));
            var result3 = publishers.GetPublisherFor(typeof(EventWithoutNamespace));
            var result4 = publishers.GetPublisherFor(typeof(IMessageInterface));

            Assert.IsNotNull(result1);
            Assert.IsNotNull(result4);
            Assert.IsNull(result2);
            Assert.IsNull(result3);
        }

        [Theory]
        [TestCase("")]
        [TestCase(null)]
        public void Should_support_empty_namespace(string eventNamespace)
        {
            var settings = new SettingsHolder();
            settings.Set<Conventions>(new Conventions());
            var routingSettings = new RoutingSettings<MessageDrivenTransportDefinition>(settings);
            routingSettings.RegisterPublisher(Assembly.GetExecutingAssembly(), eventNamespace, "someAddress");

            var publishers = ApplyPublisherRegistrations(routingSettings);

            var result1 = publishers.GetPublisherFor(typeof(BaseMessage));
            var result2 = publishers.GetPublisherFor(typeof(SubMessage));
            var result3 = publishers.GetPublisherFor(typeof(EventWithoutNamespace));
            var result4 = publishers.GetPublisherFor(typeof(IMessageInterface));

            Assert.IsNotNull(result3);
            Assert.IsNull(result1);
            Assert.IsNull(result2);
            Assert.IsNull(result4);
        }

        static Publishers ApplyPublisherRegistrations(RoutingSettings<MessageDrivenTransportDefinition> routingSettings)
        {
            var publishers = new Publishers();
            var registrations = routingSettings.Settings.Get<ConfiguredPublishers>();
            registrations.Apply(publishers);
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
    using NServiceBus;
    class EventWithNamespace : IEvent
    {
    }
}

class Event : IEvent
{
}

namespace MessageNameSpace
{
    interface IMessageInterface : IEvent
    {
    }

    class BaseMessage : IMessageInterface
    {
    }
}

namespace OtherMesagenameSpace
{
    using MessageNameSpace;

    class SubMessage : BaseMessage
    {
    }
}

class EventWithoutNamespace : IEvent
{
}
