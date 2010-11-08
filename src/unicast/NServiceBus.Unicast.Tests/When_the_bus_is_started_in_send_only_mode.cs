using System;
using NServiceBus;
using NServiceBus.Serialization;
using NServiceBus.Unicast.Transport;
using NUnit.Framework;
using Rhino.Mocks;
using SomeUserNamespace;

namespace NServiceBus.Unicast.Tests
{
    [TestFixture]
    public class When_the_bus_is_started_in_send_only_mode
    {
        UnicastBus bus;
        ITransport transport;

        [SetUp]
        public void SetUp()
        {
            transport = MockRepository.GenerateStub<ITransport>();

            bus = new UnicastBus
                      {
                          Transport = transport,
                          MessageSerializer = MockRepository.GenerateStub<IMessageSerializer>()

                      };
        }


        [Test]
        public void The_transport_should_not_be_started()
        {
            StartBus();
            transport.AssertWasNotCalled(x => x.Start(Arg<string>.Is.Anything));
        }

        [Test]
        public void An_exception_should_be_thrown_if_userdefined_messagehandlers_are_found()
        {
            bus.MessageHandlerTypes = new[] { typeof(SomeBuiltInHandler), typeof(TestMessageHandler) };

            Assert.Throws<InvalidOperationException>(StartBus);
        }

        [Test]
        public void Builtin_messagehandlers_should_be_allowed()
        {
            bus.MessageHandlerTypes = new[] { typeof(SomeBuiltInHandler) };

            Assert.DoesNotThrow(StartBus);
        }



        private void StartBus()
        {
            ((IStartableBus)bus).Start();

        }

    }

    public class SomeBuiltInHandler:IHandleMessages<SomeBuiltinMessage>
    {
        public void Handle(SomeBuiltinMessage message)
        {
            throw new NotImplementedException();
        }
    }

    public class SomeBuiltinMessage : IMessage
    {
    }
}

namespace SomeUserNamespace
{
    public class TestMessageHandler : IHandleMessages<TestMessage>
    {
        public void Handle(TestMessage message)
        {
            throw new NotImplementedException();
        }
    }
    public class TestMessage : IMessage
    {
    }
}
