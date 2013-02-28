﻿namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Linq;
    using BackwardCompatibility;
    using Contexts;
    using NUnit.Framework;
    using Rhino.Mocks;
    using Subscriptions;
    using Timeout;

    [TestFixture]
    public class When_receiving_a_regular_message : using_the_unicastbus
    {
        [Test]
        public void Should_invoke_the_registered_message_handlers()
        {
            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());
            
            RegisterMessageType<EventMessage>();
            RegisterMessageHandlerType<Handler1>();
            RegisterMessageHandlerType<Handler2>();

            ReceiveMessage(receivedMessage);


            Assert.True(Handler1.Called);
            Assert.True(Handler2.Called);
        }
    }

    [TestFixture]
    public class When_receiving_any_message : using_the_unicastbus
    {
        [Test]
        public void Should_invoke_the_registered_catch_all_handler_using_a_object_parameter()
        {
            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());

            RegisterMessageType<EventMessage>();
            RegisterMessageHandlerType<CatchAllHandler_object>();

            ReceiveMessage(receivedMessage);

            Assert.True(CatchAllHandler_object.Called);
        }
        [Test]
        public void Should_invoke_the_registered_catch_all_handler_using_a_dynamic_parameter()
        {
            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());

            RegisterMessageType<EventMessage>();
            RegisterMessageHandlerType<CatchAllHandler_dynamic>();

            ReceiveMessage(receivedMessage);

            Assert.True(CatchAllHandler_dynamic.Called);
        }


        [Test]
        public void Should_invoke_the_registered_catch_all_handler_using_a_imessage_parameter()
        {
            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());

            RegisterMessageType<EventMessage>();
            RegisterMessageHandlerType<CatchAllHandler_IMessage>();

            ReceiveMessage(receivedMessage);

            Assert.True(CatchAllHandler_IMessage.Called);
        }
    }
  
    [TestFixture]
    public class When_sending_messages_from_a_messagehandler : using_the_unicastbus
    {
        [Test]
        public void Should_set_the_related_to_header_with_the_id_of_the_current_message()
        {
            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());

            RegisterMessageType<EventMessage>();
            RegisterMessageType<CommandMessage>();
            RegisterMessageHandlerType<HandlerThatSendsAMessage>();

            ReceiveMessage(receivedMessage);

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Headers[Headers.RelatedTo] == receivedMessage.IdForCorrelation), Arg<Address>.Is.Anything));
        }
    }

    [TestFixture]
    public class When_replying_with_a_command : using_the_unicastbus
    {
        [Test]
        public void Should_not_be_allowed()
        {
            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());

            RegisterMessageType<EventMessage>();
            RegisterMessageType<CommandMessage>();
            RegisterMessageHandlerType<HandlerThatRepliesWithACommand>();

            ReceiveMessage(receivedMessage);

            messageSender.AssertWasNotCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<Address>.Is.Anything));
        }
    }

    [TestFixture]
    public class When_receiving_a_subscription_request : using_the_unicastbus
    {
        [Test]
        public void Should_register_the_subscriber()
        {
            var subscriberAddress = Address.Parse("mySubscriber");

            var subscriptionMessage = new TransportMessage
                {
                    MessageIntent = MessageIntentEnum.Subscribe,
                    ReplyToAddress = subscriberAddress
                };
           

            subscriptionMessage.Headers[Headers.SubscriptionMessageType] = typeof (EventMessage).AssemblyQualifiedName;

            var eventFired = false;
            subscriptionManager.ClientSubscribed += (sender, args) =>
            {
                Assert.AreEqual(subscriberAddress, args.SubscriberReturnAddress);
                eventFired = true;
            };


            ReceiveMessage(subscriptionMessage);

            
            Assert.AreEqual(subscriberAddress, subscriptionStorage.GetSubscriberAddressesForMessage(new[] { new MessageType(typeof(EventMessage)) }).First());
            Assert.True(eventFired);
        }
    }

    [TestFixture]
    public class When_receiving_a_message_with_the_deserialization_turned_off : using_the_unicastbus
    {
        [Test]
        public void Handlers_should_not_be_invoked()
        {
            unicastBus.SkipDeserialization = true; 

            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());

            RegisterMessageType<EventMessage>();
            RegisterMessageHandlerType<Handler1>();

            ReceiveMessage(receivedMessage);


            Assert.False(Handler1.Called);
        }
    }

    [TestFixture]
    public class When_receiving_an_event_that_is_filtered_out_by_the_subscribe_predicate : using_the_unicastbus
    {
        [Test]
        public void Should_not_invoke_the_handlers()
        {
            Handler2.Called = false;
            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());

            RegisterMessageType<EventMessage>();
            bus.Subscribe(typeof(EventMessage),m=>false);

            RegisterMessageHandlerType<Handler2>();

            ReceiveMessage(receivedMessage);

            Assert.False(Handler2.Called);
        }
    }

    [TestFixture]
    public class When_receiving_a_v3_saga_timeout_message : using_the_unicastbus
    {
        [Test]
        public void Should_set_the_newv4_flag()
        {
            var timeoutMessage = Helpers.Helpers.Serialize(new SomeTimeout());


            var mutator = new SetIsSagaMessageHeaderForV3XMessages();
            ExtensionMethods.GetHeaderAction = (o, s) =>
                {
                    if(s == TimeoutManagerHeaders.Expire)
                        return DateTime.UtcNow.ToString();
                    return "";
                };


            var flagWasSet = false;
            ExtensionMethods.SetHeaderAction = (o, s, arg3) =>
                {
                    if (s == Headers.IsSagaTimeoutMessage)
                        flagWasSet = true;

                };
            mutator.MutateIncoming(timeoutMessage);

            Assert.True(flagWasSet,"The message should be marked a saga timeoutmessage");
        }

        class SomeTimeout{}

    }


    class HandlerThatRepliesWithACommand : IHandleMessages<EventMessage>
    {
        public IBus Bus { get; set; }

        public void Handle(EventMessage message)
        {
            Bus.Reply(new CommandMessage());
        }
    }
    
    class HandlerThatSendsAMessage : IHandleMessages<EventMessage>
    {
        public IBus Bus { get; set; }

        public void Handle(EventMessage message)
        {
            Bus.Send(new CommandMessage());
        }
    }

    class CheckMesageIdHandler : IHandleMessages<EventMessage>
    {
        public static bool Called;

        public void Handle(EventMessage message)
        {
            Called = true;
        }
    }

    class Handler1:IHandleMessages<EventMessage>
    {
        public static bool Called;

        public void AHelperMethodThatTakesTheMessageAsArgument(EventMessage message)
        {
            
        }

        public void Handle(EventMessage message)
        {
            Called = true;
        }
    }

    class Handler2 : IHandleMessages<EventMessage>
    {
        public static bool Called;

        public void Handle(EventMessage message)
        {
            Called = true;
        }
    }

    public class CatchAllHandler_object:IHandleMessages<object>
    {
        public static bool Called;

        public void Handle(object message)
        {
            Called = true;
        }
    }

    public class CatchAllHandler_dynamic : IHandleMessages<object>
    {
        public static bool Called;

        public void Handle(dynamic message)
        {
            Called = true;
        }
    }


    public class CatchAllHandler_IMessage : IHandleMessages<IMessage>
    {
        public static bool Called;

        public void Handle(IMessage message)
        {
            Called = true;
        }
    }
}
