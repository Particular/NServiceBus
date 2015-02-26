namespace NServiceBus.Core.Tests.AutomaticSubscriptions
{
    using System.Linq;
    using NUnit.Framework;
    using Unicast.Routing;
    using Unicast.Tests.Contexts;

    [TestFixture]
    public class When_starting_an_endpoint_with_autoSubscribe_turned_on : AutoSubscriptionContext
    {
        [Test]
        public void Should_not_autoSubscribe_commands()
        {
            var commandEndpointAddress = "CommandEndpoint@localhost";

            RegisterMessageType<CommandMessage>(commandEndpointAddress);
            RegisterMessageHandlerType<CommandMessageHandler>();


            Assert.False(autoSubscriptionStrategy.GetEventsToSubscribe().Any(), "Commands should not be auto subscribed");
        }


        [Test]
        public void Should_not_autoSubscribe_messages_by_default()
        {
            var endpointAddress = "MyEndpoint@localhost";

            RegisterMessageType<MyMessage>(endpointAddress);
            RegisterMessageHandlerType<MyMessageHandler>();

            Assert.False(autoSubscriptionStrategy.GetEventsToSubscribe().Any(), "Plain messages should not be auto subscribed by default");
        }

        [Test]
        public void Should_not_autoSubscribe_messages_unless_asked_to_by_the_users()
        {
            var endpointAddress = "MyEndpoint@localhost";

            autoSubscriptionStrategy.SubscribePlainMessages = true;

            RegisterMessageType<MyMessage>(endpointAddress);
            RegisterMessageHandlerType<MyMessageHandler>();

            Assert.True(autoSubscriptionStrategy.GetEventsToSubscribe().Any(), "Plain messages should be auto subscribed if users wants them to be");
        }


        [Test]
        public void Should_autoSubscribe_messages_without_routing_if_configured_to_do_so()
        {
            autoSubscriptionStrategy.MessageRouter = new StaticMessageRouter(new[] { typeof(EventMessage) });

            RegisterMessageHandlerType<EventMessageHandler>();

            autoSubscriptionStrategy.DoNotRequireExplicitRouting = true;
            Assert.True(autoSubscriptionStrategy.GetEventsToSubscribe().Any(), "Events without routing should be auto subscribed if asked for");
        }

        class MyMessage : IMessage
        {

        }

        class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public void Handle(MyMessage message)
            {
                throw new System.NotImplementedException();
            }
        }


        public class EventMessageHandler : IHandleMessages<EventMessage>
        {
            public void Handle(EventMessage message)
            {
                throw new System.NotImplementedException();
            }
        }
        public class CommandMessageHandler : IHandleMessages<CommandMessage>
        {
            public void Handle(CommandMessage message)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}