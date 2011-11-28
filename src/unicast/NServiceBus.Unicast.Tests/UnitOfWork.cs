namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using NUnit.Framework;
    using UnitOfWork;

    [TestFixture]
    public class When_processing_a_message_successfully : using_the_unicastbus
    {
        [Test]
        public void Should_invoke_the_uow_begin_and_end()
        {
            var beginCalled = false;
            var endCalled = false;

            var uow = new TestUnitOfWork
                          {
                              OnBegin = () =>
                                            {
                                                beginCalled = true;
                                                Assert.False(endCalled);
                                            },
                              OnEnd = (ex) => { Assert.Null(ex); endCalled = true; }
                          };

            RegisterUow(uow);
            ReceiveMessage(Helpers.Helpers.EmptyTransportMessage());

            Assert.True(beginCalled);
            Assert.True(endCalled);
        }


     
    }


    [TestFixture]
    public class When_processing_a_message_fails : using_the_unicastbus
    {
        [Test]
        public void Should_pass_the_exception_to_the_uow_end()
        {
            RegisterMessageType<MessageThatBlowsUp>();

            unicastBus.MessageHandlerTypes = new[] { typeof(MessageThatBlowsUpHandler) };

            var endCalled = false;

            var uow = new TestUnitOfWork
            {
                OnEnd = (ex) => { Assert.NotNull(ex); endCalled = true; }
            };

            RegisterUow(uow);
            ReceiveMessage(Helpers.Helpers.Serialize(new MessageThatBlowsUp()));

            Assert.True(endCalled);
        }



    }

    public class MessageThatBlowsUpHandler:IHandleMessages<MessageThatBlowsUp>
    {
        public void Handle(MessageThatBlowsUp message)
        {
            throw new Exception("Generated failure");
        }
    }

    public class MessageThatBlowsUp:IMessage
    {
    }

    public class TestUnitOfWork:IManageUnitsOfWork
    {
        public Action<Exception> OnEnd = (ex) => { };
        public Action OnBegin = () => { };

        public void Begin()
        {
            OnBegin();
        }

        public void End(Exception ex = null)
        {
            OnEnd(ex);
        }
    }
}