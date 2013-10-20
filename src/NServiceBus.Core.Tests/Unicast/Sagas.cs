namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using Saga;

    [TestFixture]
    public class When_receiving_a_message_that_is_configure_to_start_a_saga : with_sagas
    {
        [Test]
        public void Should_create_a_new_saga_if_no_existing_instance_is_found()
        {
            RegisterSaga<MySaga>();

            ReceiveMessage(new StartMessage());

            Assert.AreEqual(1, persister.CurrentSagaEntities.Keys.Count());
        }

        class MySaga : Saga<MySagaData>, IAmStartedByMessages<StartMessage>
        {
            public void Handle(StartMessage message){}
        }

        class MySagaData : ContainSagaData{}

        class StartMessage : IMessage{}
    }

    [TestFixture]
    public class When_receiving_a_message_that_is_handled_by_a_saga : with_sagas
    {
        [Test]
        public void Should_find_existing_instance_by_id_if_saga_header_is_found()
        {
            var sagaId = Guid.NewGuid();

            RegisterSaga<MySaga>();
            RegisterExistingSagaEntity(new MySagaData{ Id = sagaId});

            ReceiveMessage(new MessageThatHitsExistingSaga(), new Dictionary<string, string> { { Headers.SagaId, sagaId.ToString() } });

            Assert.AreEqual(1,persister.CurrentSagaEntities.Count(), "Existing saga should be found");

            Assert.AreEqual("Test", ((MySagaData)persister.CurrentSagaEntities[sagaId].SagaEntity).SomeValue, "Entity should be updated");
        }

        class MySaga : Saga<MySagaData>, IHandleMessages<MessageThatHitsExistingSaga>
        {
            public void Handle(MessageThatHitsExistingSaga message)
            {
                Data.SomeValue = "Test";
            }
        }

        class MySagaData : ContainSagaData 
        {
            public string SomeValue { get; set; }
        }

        class MessageThatHitsExistingSaga : IMessage { }
    }

    [TestFixture]
    public class When_receiving_a_message_that_is_not_set_to_start_a_saga : with_sagas
    {
        [Test,Ignore("Until we add the check in saga persitence behavior to Disable invocation if message are not allowed to start a new saga"))]
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

        class SagaNotFoundHandler:IHandleSagaNotFound
        {
            public void Handle(object message)
            {
            }
        }

        class MySagaData : ContainSagaData
        {
            public string SomeValue { get; set; }
        }

        class MessageThatMissesSaga : IMessage { }
    }

}

