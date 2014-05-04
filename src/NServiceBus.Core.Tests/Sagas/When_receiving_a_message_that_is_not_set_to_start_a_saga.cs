namespace NServiceBus.Unicast.Tests
{
    using System.Linq;
    using NUnit.Framework;
    using Saga;

    [TestFixture]
    public class When_receiving_a_message_that_is_not_set_to_start_a_saga : with_sagas
    {
        [Test]
        public void Should_invoke_saga_not_found_handlers_if_no_saga_instance_is_found()
        {
            RegisterSaga<MySaga>();

            var invoked = false;

            FuncBuilder.Register<IHandleSagaNotFound>(() =>
            {
                invoked = true;
                return new SagaNotFoundHandler();
            });

            ReceiveMessage(new MessageThatMissesSaga());

            Assert.True(invoked, "Not found handler should be invoked");

            Assert.AreEqual(0, persister.CurrentSagaEntities.Count(), "No saga should be stored");
        }

        class MySaga : Saga<MySagaData>, IHandleMessages<MessageThatMissesSaga>
        {
            public void Handle(MessageThatMissesSaga message)
            {
                Assert.Fail("Handler should not be invoked");
            }
        }

        class SagaNotFoundHandler : IHandleSagaNotFound
        {
            public void Handle(object message)
            {
            }
        }

        class MySagaData : ContainSagaData
        {
        }

        class MessageThatMissesSaga : IMessage { }
    }
}