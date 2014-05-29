namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using Saga;

    [TestFixture]
    class When_completing_a_saga : with_sagas
    {
        [Test]
        public void Should_not_be_persisted_if_completed_right_away()
        {
            RegisterSaga<MySaga>();

            ReceiveMessage(new MessageThatStartsSaga());

            Assert.AreEqual(0, persister.CurrentSagaEntities.Count(), "No saga should be stored");
        }

        [Test]
        public void Should_be_removed_from_storage_if_persistent()
        {
            var sagaId = Guid.NewGuid();

            RegisterSaga<MySaga>(new MySagaData { Id = sagaId });

            ReceiveMessage(new MessageThatStartsSaga(), new Dictionary<string, string> { { Headers.SagaId, sagaId.ToString() } });

            Assert.AreEqual(0, persister.CurrentSagaEntities.Count(), "No saga should be stored");
        }


        class MySaga : Saga<MySagaData>, IAmStartedByMessages<MessageThatStartsSaga>
        {
            public void Handle(MessageThatStartsSaga message)
            {
                MarkAsComplete();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
            {
            }
        }

        class MySagaData : ContainSagaData
        {
        }

        class MessageThatStartsSaga : IMessage { }
    }
}