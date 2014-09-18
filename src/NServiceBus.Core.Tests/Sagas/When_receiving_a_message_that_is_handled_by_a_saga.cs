namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using Saga;

    [TestFixture]
    class When_receiving_a_message_that_is_handled_by_a_saga : with_sagas
    {
        [Test]
        public void Should_find_existing_instance_by_id_if_saga_header_is_found()
        {
            var sagaId = Guid.NewGuid();

            RegisterSaga<MySaga>(new MySagaData { Id = sagaId });

            ReceiveMessage(new MessageThatHitsExistingSaga(), new Dictionary<string, string> { { Headers.SagaId, sagaId.ToString() } }, mapper: MessageMapper);

            Assert.AreEqual(1, persister.CurrentSagaEntities.Count(), "Existing saga should be found");

            Assert.AreEqual("Test", ((MySagaData)persister.CurrentSagaEntities[sagaId].SagaEntity).SomeValue, "Entity should be updated");
        }

        [Test]
        public void Should_find_existing_instance_by_property()
        {
            var sagaId = Guid.NewGuid();
            var correlationId = Guid.NewGuid();

            RegisterSaga<MySaga>(new MySagaData { Id = sagaId, PropertyThatCorrelatesToMessage = correlationId });

            ReceiveMessage(new MessageThatHitsExistingSaga { PropertyThatCorrelatesToSaga = correlationId }, mapper: MessageMapper);

            Assert.AreEqual(1, persister.CurrentSagaEntities.Count(), "Existing saga should be found");

            Assert.AreEqual("Test", ((MySagaData)persister.CurrentSagaEntities[sagaId].SagaEntity).SomeValue, "Entity should be updated");
        }


        [Test]
        public void Should_enrich_the_audit_data_with_the_saga_type_and_id()
        {
            var sagaId = Guid.NewGuid();
            var correlationId = Guid.NewGuid();

            RegisterSaga<MySaga>(new MySagaData { Id = sagaId, PropertyThatCorrelatesToMessage = correlationId });

            ReceiveMessage(new MessageThatHitsExistingSaga { PropertyThatCorrelatesToSaga = correlationId }, mapper: MessageMapper);

            var sagaAuditTrail = AuditedMessage.Headers["NServiceBus.InvokedSagas"];

            var sagas = sagaAuditTrail.Split(';');

            var sagaInfo = sagas.Single();

            Assert.AreEqual(typeof(MySaga).FullName,sagaInfo.Split(':').First());
            Assert.AreEqual(sagaId.ToString(), sagaInfo.Split(':').Last());
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