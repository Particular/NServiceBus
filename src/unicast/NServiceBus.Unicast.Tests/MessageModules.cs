namespace NServiceBus.Unicast.Tests
{
    using System;
    using NUnit.Framework;
    using SomeUserNamespace;
    using Transport;

    [TestFixture]
    public class When_a_message_if_forwarded_via_the_fault_manager : using_the_unicastbus
    {
        [Test]
        public void Should_invoke_begin_and_end_message()
        {
            var messageModule = new StubMessageModule();
            bool beginCalled = false;

            messageModule.OnBegin = () => { beginCalled = true; };

            MessageModules.Add(messageModule);

            RegisterMessageType<CommandMessage>();

            Transport.SimulateMessageReceived(new TransportMessage());
            
            
            

            Assert.True(beginCalled);
        }

        [Test]
        public void Should_not_invoke_handle_error()
        {
        }

    }

    public class StubMessageModule:IMessageModule
    {
        public Action OnBegin = () => { };
        public void HandleBeginMessage()
        {
            OnBegin();
        }

        public void HandleEndMessage()
        {
            
        }

        public void HandleError()
        {
        }
    }
}