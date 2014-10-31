namespace NServiceBus.Unicast.Tests
{
    using Contexts;
    using NUnit.Framework;
    using Rhino.Mocks;
    using Saga;

    [TestFixture]
    class When_receiving_a_regular_message : using_the_unicastBus
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
    class When_receiving_any_message : using_the_unicastBus
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
        public void Should_throw_when_there_are_no_registered_message_handlers()
        {
            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());
            RegisterMessageType<EventMessage>();
            ReceiveMessage(receivedMessage);
            Assert.IsNotNull(ResultingException, "When no handlers are found and a message ends up in the endpoint, an exception should be thrown");
            Assert.That(ResultingException.GetBaseException().Message, Contains.Substring(typeof(EventMessage).ToString()), "The exception message should be meaningful and should inform the user the message type for which a handler could not be found.");
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
     class When_sending_messages_from_a_messageHandler : using_the_unicastBus
    {
        [Test]
        public void Should_set_the_related_to_header_with_the_id_of_the_current_message()
        {
            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());

            receivedMessage.CorrelationId = receivedMessage.Id;

            RegisterMessageType<EventMessage>();
            RegisterMessageType<CommandMessage>();
            RegisterMessageHandlerType<HandlerThatSendsAMessage>();

            ReceiveMessage(receivedMessage);

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Headers[Headers.RelatedTo] == receivedMessage.CorrelationId), Arg<SendOptions>.Is.Anything));
        }
    }

    [TestFixture]
    class When_replying_with_a_command : using_the_unicastBus
    {
        [Test]
        public void Should_not_be_allowed()
        {
            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());

            RegisterMessageType<EventMessage>();
            RegisterMessageType<CommandMessage>();
            RegisterMessageHandlerType<HandlerThatRepliesWithACommand>();

            ReceiveMessage(receivedMessage);

            messageSender.AssertWasNotCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<SendOptions>.Is.Anything));
        }
    }

    [TestFixture]
    class When_receiving_a_v3_saga_timeout_message 
    {
        [Test,Ignore("Move to a acceptance test")]
        public void Should_set_the_newV4_flag()
        {
            //var sagaId = Guid.NewGuid();

            //RegisterSaga<MySaga>(new MySagaData
            //{
            //    Id = sagaId
            //});

            //ReceiveMessage(new MyTimeout(), new Dictionary<string, string>
            //{
            //    {Headers.NServiceBusVersion, "3.3.8"},
            //    {Headers.SagaId, sagaId.ToString()},
            //    {TimeoutManagerHeaders.Expire, "2013-06-20 03:41:00:188412 Z"}
            //}, mapper: MessageMapper);

            //Assert.AreEqual(1, persister.CurrentSagaEntities.Count, "Existing saga should be found");
            //Assert.True(((MySagaData)persister.CurrentSagaEntities[sagaId].SagaEntity).TimeoutCalled, "Timeout method should be invoked");
        }

        class MySaga : Saga<MySagaData>, IHandleTimeouts<MyTimeout>, IHandleMessages<MyTimeout>
        {
            public void Timeout(MyTimeout timeout)
            {
                Data.TimeoutCalled = true;
            }

            public void Handle(MyTimeout message)
            {
                Assert.Fail("Regular handler should not be invoked");
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
            {
            }
        }

        class MySagaData : ContainSagaData
        {
            public bool TimeoutCalled { get; set; }
        }

        class MyTimeout : IMessage { }
    
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

    class Handler1:IHandleMessages<EventMessage>
    {
        public static bool Called;

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
