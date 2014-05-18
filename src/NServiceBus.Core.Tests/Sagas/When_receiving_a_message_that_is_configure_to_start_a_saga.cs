namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using Saga;

    [TestFixture]
    class When_receiving_a_message_that_is_configure_to_start_a_saga : with_sagas
    {
        [Test]
        public void Should_create_a_new_saga_if_no_existing_instance_is_found()
        {
            RegisterSaga<MySaga>();

            ReceiveMessage(new StartMessage());

            Assert.AreEqual(1, persister.CurrentSagaEntities.Keys.Count());
        }

        [Test]
        public void Should_create_a_new_saga_if_no_existing_instance_is_found_for_interface_based_messages()
        {
            RegisterSaga<MySaga>();

            RegisterMessageType<StartMessageThatIsAnInterface>();
            ReceiveMessage(MessageMapper.CreateInstance<StartMessageThatIsAnInterface>());

            Assert.AreEqual(1, persister.CurrentSagaEntities.Keys.Count());
        }

        [Test]
        public void Should_hit_existing_saga_if_one_is_found()
        {
            var sagaId = Guid.NewGuid();

            RegisterSaga<MySaga>(new MySagaData { Id = sagaId });

            ReceiveMessage(new StartMessage(), new Dictionary<string, string> { { Headers.SagaId, sagaId.ToString() } });

            Assert.AreEqual(1, persister.CurrentSagaEntities.Keys.Count());
        }

        class MySaga : Saga<MySagaData>, IAmStartedByMessages<StartMessage>, IAmStartedByMessages<StartMessageThatIsAnInterface>
        {
            public void Handle(StartMessage message) { }
            public void Handle(StartMessageThatIsAnInterface message) { }
        }

        class MySagaData : ContainSagaData { }

        class StartMessage : IMessage { }
    }

    public interface StartMessageThatIsAnInterface : IMessage { }
}