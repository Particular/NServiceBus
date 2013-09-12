namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BackwardCompatibility;
    using Contexts;
    using NUnit.Framework;
    using Rhino.Mocks;
    using Subscriptions;
    using Timeout;

    [TestFixture]
    public class When_receiving_a_regular_message : using_the_unicastBus
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

        [Test]
        public void Should_throw_when_there_are_no_registered_message_handlers()
        {
            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());
            RegisterMessageType<EventMessage>();
            ReceiveMessage(receivedMessage);
            Assert.IsTrue(ResultingException != null, "When no handlers are found and a message ends up in the endpoint, an exception should be thrown");
            Assert.IsTrue(ResultingException.InnerException.GetType() == typeof(InvalidOperationException), "The inner exception must contain an InvalidOperationException");
            Assert.IsTrue(ResultingException.InnerException.Message.Contains(typeof(EventMessage).ToString()), "The exception message should be meaningful and should inform the user the message type for which a handler could not be found.");
        }
    }

    [TestFixture]
    public class When_receiving_any_message : using_the_unicastBus
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
        public void Should_invoke_the_registered_catch_all_handler_using_a_iMessage_parameter()
        {
            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());

            RegisterMessageType<EventMessage>();
            RegisterMessageHandlerType<CatchAllHandler_IMessage>();

            ReceiveMessage(receivedMessage);

            Assert.True(CatchAllHandler_IMessage.Called);
        }
    }
  
    [TestFixture]
    public class When_sending_messages_from_a_messageHandler : using_the_unicastBus
    {
        [Test]
        public void Should_set_the_related_to_header_with_the_id_of_the_current_message()
        {
            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());

            RegisterMessageType<EventMessage>();
            RegisterMessageType<CommandMessage>();
            RegisterMessageHandlerType<HandlerThatSendsAMessage>();

            ReceiveMessage(receivedMessage);

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Headers[Headers.RelatedTo] == receivedMessage.CorrelationId), Arg<Address>.Is.Anything));
        }
    }

    [TestFixture]
    public class When_replying_with_a_command : using_the_unicastBus
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
    public class When_receiving_a_subscription_request : using_the_unicastBus
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
    public class When_receiving_a_message_with_the_deserialization_turned_off : using_the_unicastBus
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
    public class When_receiving_an_event_that_is_filtered_out_by_the_subscribe_predicate : using_the_unicastBus
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
    public class When_receiving_a_v3_saga_timeout_message : using_the_unicastBus
    {
        [Test]
        public void Should_set_the_newV4_flag()
        {
            var timeoutMessage = Helpers.Helpers.Serialize(new SomeTimeout());
            var mutator = new SetIsSagaMessageHeaderForV3XMessages
                {
                    Bus = new MyBus{CurrentMessageContext = new MessageContext(timeoutMessage)},
                };

            var headers = new Dictionary<string, string>();

            ExtensionMethods.GetHeaderAction = (o, s) =>
            {
                string v;
                headers.TryGetValue(s, out v);
                return v;
            };

            ExtensionMethods.SetHeaderAction = (o, s, v) =>
            {
                headers[s] = v;
            };

            Headers.SetMessageHeader(timeoutMessage, Headers.NServiceBusVersion, "3.3.8");
            Headers.SetMessageHeader(timeoutMessage, Headers.SagaId, "ded93a22-1e4b-466a-818f-a1e300cfb9d6");
            Headers.SetMessageHeader(timeoutMessage, TimeoutManagerHeaders.Expire, "2013-06-20 03:41:00:188412 Z");

            mutator.MutateIncoming(timeoutMessage);

            Assert.True(timeoutMessage.Headers.ContainsKey(Headers.IsSagaTimeoutMessage));
            Assert.AreEqual(Boolean.TrueString, timeoutMessage.Headers[Headers.IsSagaTimeoutMessage]);
        }

        class MyBus: IBus
        {
            public T CreateInstance<T>()
            {
                throw new NotImplementedException();
            }

            public T CreateInstance<T>(Action<T> action)
            {
                throw new NotImplementedException();
            }

            public object CreateInstance(Type messageType)
            {
                throw new NotImplementedException();
            }

            public void Publish<T>(params T[] messages)
            {
                throw new NotImplementedException();
            }

            public void Publish<T>(T message)
            {
                throw new NotImplementedException();
            }

            public void Publish<T>()
            {
                throw new NotImplementedException();
            }

            public void Publish<T>(Action<T> messageConstructor)
            {
                throw new NotImplementedException();
            }

            public void Subscribe(Type messageType)
            {
                throw new NotImplementedException();
            }

            public void Subscribe<T>()
            {
                throw new NotImplementedException();
            }

            public void Subscribe(Type messageType, Predicate<object> condition)
            {
                throw new NotImplementedException();
            }

            public void Subscribe<T>(Predicate<T> condition)
            {
                throw new NotImplementedException();
            }

            public void Unsubscribe(Type messageType)
            {
                throw new NotImplementedException();
            }

            public void Unsubscribe<T>()
            {
                throw new NotImplementedException();
            }

            public ICallback SendLocal(params object[] messages)
            {
                throw new NotImplementedException();
            }

            public ICallback SendLocal(object message)
            {
                throw new NotImplementedException();
            }

            public ICallback SendLocal<T>(Action<T> messageConstructor)
            {
                throw new NotImplementedException();
            }

            public ICallback Send(params object[] messages)
            {
                throw new NotImplementedException();
            }

            public ICallback Send(object message)
            {
                throw new NotImplementedException();
            }

            public ICallback Send<T>(Action<T> messageConstructor)
            {
                throw new NotImplementedException();
            }

            public ICallback Send(string destination, params object[] messages)
            {
                throw new NotImplementedException();
            }

            public ICallback Send(string destination, object message)
            {
                throw new NotImplementedException();
            }

            public ICallback Send(Address address, params object[] messages)
            {
                throw new NotImplementedException();
            }

            public ICallback Send(Address address, object message)
            {
                throw new NotImplementedException();
            }

            public ICallback Send<T>(string destination, Action<T> messageConstructor)
            {
                throw new NotImplementedException();
            }

            public ICallback Send<T>(Address address, Action<T> messageConstructor)
            {
                throw new NotImplementedException();
            }

            public ICallback Send(string destination, string correlationId, params object[] messages)
            {
                throw new NotImplementedException();
            }

            public ICallback Send(string destination, string correlationId, object message)
            {
                throw new NotImplementedException();
            }

            public ICallback Send(Address address, string correlationId, params object[] messages)
            {
                throw new NotImplementedException();
            }

            public ICallback Send(Address address, string correlationId, object message)
            {
                throw new NotImplementedException();
            }

            public ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor)
            {
                throw new NotImplementedException();
            }

            public ICallback Send<T>(Address address, string correlationId, Action<T> messageConstructor)
            {
                throw new NotImplementedException();
            }

            public ICallback SendToSites(IEnumerable<string> siteKeys, params object[] messages)
            {
                throw new NotImplementedException();
            }

            public ICallback SendToSites(IEnumerable<string> siteKeys, object message)
            {
                throw new NotImplementedException();
            }

            public ICallback Defer(TimeSpan delay, params object[] messages)
            {
                throw new NotImplementedException();
            }

            public ICallback Defer(TimeSpan delay, object message)
            {
                throw new NotImplementedException();
            }

            public ICallback Defer(DateTime processAt, params object[] messages)
            {
                throw new NotImplementedException();
            }

            public ICallback Defer(DateTime processAt, object message)
            {
                throw new NotImplementedException();
            }

            public void Reply(params object[] messages)
            {
                throw new NotImplementedException();
            }

            public void Reply(object message)
            {
                throw new NotImplementedException();
            }

            public void Reply<T>(Action<T> messageConstructor)
            {
                throw new NotImplementedException();
            }

            public void Return<T>(T errorEnum)
            {
                throw new NotImplementedException();
            }

            public void HandleCurrentMessageLater()
            {
                throw new NotImplementedException();
            }

            public void ForwardCurrentMessageTo(string destination)
            {
                throw new NotImplementedException();
            }

            public void DoNotContinueDispatchingCurrentMessageToHandlers()
            {
                throw new NotImplementedException();
            }

            public IDictionary<string, string> OutgoingHeaders { get; private set; }
            public IMessageContext CurrentMessageContext { get; set; }
            public IInMemoryOperations InMemory { get; private set; }
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

    class CheckMessageIdHandler : IHandleMessages<EventMessage>
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
