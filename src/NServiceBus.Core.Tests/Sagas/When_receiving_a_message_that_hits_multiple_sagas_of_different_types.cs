namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Linq;
    using NUnit.Framework;
    using Saga;

    [TestFixture]
    class When_receiving_a_message_that_hits_multiple_sagas_of_different_types : with_sagas
    {
        [Test]
        public void Should_invoke_all_of_them()
        {
            var sagaId = Guid.NewGuid();
            var sagaId2 = Guid.NewGuid();
            var correlationId = Guid.NewGuid();

            RegisterSaga<MySaga>(new MySagaData { Id = sagaId, PropertyThatCorrelatesToMessage = correlationId });

            RegisterSaga<MySaga2>(new MySagaData2 { Id = sagaId2, PropertyThatCorrelatesToMessage = correlationId });

            ReceiveMessage(new MessageThatHitsExistingSaga { PropertyThatCorrelatesToSaga = correlationId }, mapper: MessageMapper);

            Assert.AreEqual(2, persister.CurrentSagaEntities.Count(), "Existing saga should be found");

            Assert.AreEqual("Test", ((MySagaData)persister.CurrentSagaEntities[sagaId].SagaEntity).SomeValue, "Entity should be updated");
            Assert.AreEqual("Test", ((MySagaData2)persister.CurrentSagaEntities[sagaId2].SagaEntity).SomeValue, "Entity should be updated");
        }


        class MySaga2 : Saga<MySagaData2>, IHandleMessages<MessageThatHitsExistingSaga>
        {
            public void Handle(MessageThatHitsExistingSaga message)
            {
                Data.SomeValue = "Test";
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData2> mapper)
            {
                mapper.ConfigureMapping<MessageThatHitsExistingSaga>(m => m.PropertyThatCorrelatesToSaga)
                    .ToSaga(s => s.PropertyThatCorrelatesToMessage);
            }

        }

        class MySagaData2 : ContainSagaData
        {
            public string SomeValue { get; set; }
            public Guid PropertyThatCorrelatesToMessage { get; set; }
        }

        class MySaga : Saga<MySagaData>, IHandleMessages<MessageThatHitsExistingSaga>
        {
            public void Handle(MessageThatHitsExistingSaga message)
            {
                Data.SomeValue = "Test";
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
            {
                mapper.ConfigureMapping<MessageThatHitsExistingSaga>(m => m.PropertyThatCorrelatesToSaga)
                    .ToSaga(s => s.PropertyThatCorrelatesToMessage);
            }

        }

        class MySagaData : ContainSagaData
        {
            public string SomeValue { get; set; }
            public Guid PropertyThatCorrelatesToMessage { get; set; }
        }

        class MessageThatHitsExistingSaga : IMessage
        {
            public Guid PropertyThatCorrelatesToSaga { get; set; }
        }
    }
}