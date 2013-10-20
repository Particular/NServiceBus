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

            Assert.AreEqual(1, persister.CurrentSagaEntities.Keys.Count());
        }

        class MySaga : Saga<MySagaData>, IHandleMessages<MessageThatHitsExistingSaga>
        {
            public void Handle(MessageThatHitsExistingSaga message) { }
        }

        class MySagaData : ContainSagaData { }

        class MessageThatHitsExistingSaga : IMessage { }
    }


}

