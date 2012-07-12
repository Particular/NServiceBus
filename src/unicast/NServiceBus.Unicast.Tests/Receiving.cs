using System;

namespace NServiceBus.Unicast.Tests
{
    using Contexts;
    using NUnit.Framework;
    using Rhino.Mocks;
    using Saga;
    using Transport;

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
    public class When_receiving_a_message_with_an_original_Id : using_the_unicastbus
    {
        [Test]
        public void Should_use_the_original_id_as_message_id()
        {
            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());
            var originalId = Guid.NewGuid();
            receivedMessage.Headers[TransportHeaderKeys.OriginalId] = originalId.ToString();

            // Receiving a message (into the pipeline)
            RegisterMessageType<EventMessage>();
            RegisterMessageHandlerType<CheckMesageIdHandler>();
            ReceiveMessage(receivedMessage);
            
            receivedMessage.Id = receivedMessage.GetOriginalId();
            
            Assert.AreEqual(receivedMessage.Id, originalId.ToString());
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


            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Headers[Monitoring.Headers.RelatedTo] == receivedMessage.IdForCorrelation), Arg<Address>.Is.Anything));
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
