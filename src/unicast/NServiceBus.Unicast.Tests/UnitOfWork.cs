namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using NUnit.Framework;
    using UnitOfWork;

    [TestFixture]
    public class When_a_uow_end_throws : using_the_unicastbus
    {
        [Test]
        public void Should_invoke_end_if_begin_was_invoked()
        {
            var firstEndCalled = false;
            var throwableEndCalled = false;
            var lastEndCalled = false;

            var firstUoW = new TestUnitOfWork
            {
                OnEnd = ex => { firstEndCalled = true; }
            };
            var throwableUoW = new TestUnitOfWork
            {
                OnEnd = ex =>
                            {
                                throwableEndCalled = true;
                                throw new Exception();
                            }
            };
            var lastUoW = new TestUnitOfWork
            {
                OnEnd = ex => { lastEndCalled = true; }
            };
            RegisterUow(firstUoW);
            RegisterUow(throwableUoW);
            RegisterUow(lastUoW);
            ReceiveMessage(Helpers.Helpers.EmptyTransportMessage());

            Assert.True(firstEndCalled);
            Assert.True(throwableEndCalled);
            Assert.True(lastEndCalled);
        }

        [Test]
        public void Should_invoke_each_end_only_once()
        {
            var firstEndCalled = 0;
            var throwableEndCalled = 0;
            var lastEndCalled = 0;

            var firstUoW = new TestUnitOfWork
            {
                OnEnd = ex => { firstEndCalled++; }
            };
            var throwableUoW = new TestUnitOfWork
            {
                OnEnd = ex =>
                {
                    throwableEndCalled++;
                    throw new Exception();
                }
            };
            var lastUoW = new TestUnitOfWork
            {
                OnEnd = ex => { lastEndCalled++; }
            };
            RegisterUow(firstUoW);
            RegisterUow(throwableUoW);
            RegisterUow(lastUoW);
            ReceiveMessage(Helpers.Helpers.EmptyTransportMessage());

            Assert.AreEqual(1, firstEndCalled);
            Assert.AreEqual(1, throwableEndCalled);
            Assert.AreEqual(1, lastEndCalled);
        }
    }

    [TestFixture]
    public class When_a_uow_begin_throws : using_the_unicastbus
    {
        [Test]
        public void Should_not_invoke_end_if_begin_was_not_invoked()
        {
            var firstEndCalled = false;
            var throwableEndCalled = false;
            var lastEndCalled = false;

            var firstUoW = new TestUnitOfWork
            {
                OnEnd = ex => { firstEndCalled = true; }
            };
            var throwableUoW = new TestUnitOfWork
            {
                OnBegin = () =>
                {
                    throw new Exception();
                },
                OnEnd = ex => { throwableEndCalled = true; }
            };
            var lastUoW = new TestUnitOfWork
            {
                OnEnd = ex => { lastEndCalled = true; }
            };
            RegisterUow(firstUoW);
            RegisterUow(throwableUoW);
            RegisterUow(lastUoW);
            ReceiveMessage(Helpers.Helpers.EmptyTransportMessage());

            Assert.True(firstEndCalled);
            Assert.True(throwableEndCalled);
            Assert.False(lastEndCalled);
        }
    }

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