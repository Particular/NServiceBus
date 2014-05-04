namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using Rhino.Mocks;
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
        public interface StartMessageThatIsAnInterface : IMessage { }
    }

    [TestFixture]
    public class When_receiving_a_message_that_is_handled_by_a_saga : with_sagas
    {
        [Test]
        public void Should_find_existing_instance_by_id_if_saga_header_is_found()
        {
            var sagaId = Guid.NewGuid();

            RegisterSaga<MySaga>(new MySagaData { Id = sagaId });

            ReceiveMessage(new MessageThatHitsExistingSaga(), new Dictionary<string, string> { { Headers.SagaId, sagaId.ToString() } });

            Assert.AreEqual(1, persister.CurrentSagaEntities.Count(), "Existing saga should be found");

            Assert.AreEqual("Test", ((MySagaData)persister.CurrentSagaEntities[sagaId].SagaEntity).SomeValue, "Entity should be updated");
        }

        [Test]
        public void Should_find_existing_instance_by_property()
        {
            var sagaId = Guid.NewGuid();
            var correlationId = Guid.NewGuid();

            RegisterSaga<MySaga>(new MySagaData { Id = sagaId, PropertyThatCorrelatesToMessage = correlationId });

            ReceiveMessage(new MessageThatHitsExistingSaga { PropertyThatCorrelatesToSaga = correlationId });

            Assert.AreEqual(1, persister.CurrentSagaEntities.Count(), "Existing saga should be found");

            Assert.AreEqual("Test", ((MySagaData)persister.CurrentSagaEntities[sagaId].SagaEntity).SomeValue, "Entity should be updated");
        }


        [Test]
        public void Should_enrich_the_audit_data_with_the_saga_type_and_id()
        {
            var sagaId = Guid.NewGuid();
            var correlationId = Guid.NewGuid();

            RegisterSaga<MySaga>(new MySagaData { Id = sagaId, PropertyThatCorrelatesToMessage = correlationId });

            ReceiveMessage(new MessageThatHitsExistingSaga { PropertyThatCorrelatesToSaga = correlationId });

            var sagaAuditTrail = AuditedMessage.Headers[Headers.InvokedSagas];

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

            public override void ConfigureHowToFindSaga()
            {
                ConfigureMapping<MessageThatHitsExistingSaga>(m => m.PropertyThatCorrelatesToSaga)
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

    [TestFixture]
    public class When_receiving_a_timeout_message : with_sagas
    {
        [Test]
        public void Should_invoke_timeout_method_even_if_there_is_a_message_handler_as_well()
        {
            var sagaId = Guid.NewGuid();

            RegisterSaga<MySaga>(new MySagaData { Id = sagaId });

            ReceiveMessage(new MyTimeout(), new Dictionary<string, string>
            {
                { Headers.SagaId, sagaId.ToString() },
                {Headers.IsSagaTimeoutMessage, true.ToString() }
            });

            Assert.AreEqual(1, persister.CurrentSagaEntities.Count(), "Existing saga should be found");

            Assert.True(((MySagaData)persister.CurrentSagaEntities[sagaId].SagaEntity).TimeoutCalled, "Timeout method should be invoked");
        }


        class MySaga : Saga<MySagaData>, IHandleTimeouts<MyTimeout>, IHandleMessages<MyTimeout>
        {
            public void Timeout(MyTimeout timeout)
            {
                Data.TimeoutCalled = true;
            }

            public void Handle(MyTimeout message)
            {
                Assert.Fail("Regular handler should not be invoked");
            }
        }

        class MySagaData : ContainSagaData
        {
            public bool TimeoutCalled { get; set; }
        }

        class MyTimeout : IMessage { }
    }

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

    [TestFixture]
    public class When_completing_a_saga : with_sagas
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
        }

        class MySagaData : ContainSagaData
        {
        }

        class MessageThatStartsSaga : IMessage { }
    }

    [TestFixture]
    public class When_receiving_a_message_that_hits_multiple_sagas_of_different_types : with_sagas
    {
        [Test]
        public void Should_invoke_all_of_them()
        {
            var sagaId = Guid.NewGuid();
            var sagaId2 = Guid.NewGuid();
            var correlationId = Guid.NewGuid();

            RegisterSaga<MySaga>(new MySagaData { Id = sagaId, PropertyThatCorrelatesToMessage = correlationId });

            RegisterSaga<MySaga2>(new MySagaData2 { Id = sagaId2, PropertyThatCorrelatesToMessage = correlationId });

            ReceiveMessage(new MessageThatHitsExistingSaga { PropertyThatCorrelatesToSaga = correlationId });

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

            public override void ConfigureHowToFindSaga()
            {
                ConfigureMapping<MessageThatHitsExistingSaga>(m => m.PropertyThatCorrelatesToSaga)
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

            public override void ConfigureHowToFindSaga()
            {
                ConfigureMapping<MessageThatHitsExistingSaga>(m => m.PropertyThatCorrelatesToSaga)
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


    [TestFixture]
    public class When_using_custom_finders_to_find_sagas : with_sagas
    {
        [Test]
        public void Should_use_the_most_specific_finder_first_when_receiving_a_message()
        {
            RegisterSaga<SagaWithDerivedMessage>();
            RegisterCustomFinder<MyFinderForBaseClass>();
            RegisterCustomFinder<MyFinderForFoo2>();
          
            ReceiveMessage(new Foo2());

            Assert.AreEqual(1, persister.CurrentSagaEntities.Count(), "Existing saga should be found");

            var sagaData = (MySagaData) persister.CurrentSagaEntities.First().Value.SagaEntity;

            Assert.AreEqual(typeof(MyFinderForFoo2).FullName,sagaData.SourceFinder);

        }

        [Test]
        public void Should_use_base_class_finder_if_needed()
        {
            RegisterSaga<SagaWithDerivedMessage>();
            RegisterCustomFinder<MyFinderForBaseClass>();

            ReceiveMessage(new Foo2());

            Assert.AreEqual(1, persister.CurrentSagaEntities.Count(), "Existing saga should be found");

            var sagaData = (MySagaData)persister.CurrentSagaEntities.First().Value.SagaEntity;

            Assert.AreEqual(typeof(MyFinderForBaseClass).FullName, sagaData.SourceFinder);

        }

        class SagaWithDerivedMessage : Saga<MySagaData>, IHandleMessages<Foo2>
        {
            public void Handle(Foo2 message)
            {
            }
        }
        class MySagaData : ContainSagaData
        {
            public string SourceFinder { get; set; }
        }

        class Foo2 : Foo
        {
        }
        abstract class Foo : IMessage
        {
        }

        class MyFinderForBaseClass : IFindSagas<MySagaData>.Using<Foo>
        {
            public MySagaData FindBy(Foo message)
            {
                return new MySagaData { SourceFinder = typeof(MyFinderForBaseClass).FullName };
            }
        }

        class MyFinderForFoo2 : IFindSagas<MySagaData>.Using<Foo2>
        {
            public MySagaData FindBy(Foo2 message)
            {
                return new MySagaData { SourceFinder = typeof(MyFinderForFoo2).FullName };
            }
        }
    }


    [TestFixture]
    public class When_sending_messages_from_a_saga : with_sagas
    {
        [Test]
        public void Should_attach_the_originating_saga_id_as_a_header()
        {
            RegisterMessageType<MessageSentFromSaga>();
            RegisterSaga<MySaga>();

            ReceiveMessage(new MessageToProcess());

            var sagaData = (MySagaData)persister.CurrentSagaEntities.First().Value.SagaEntity;

            messageSender.AssertWasCalled(x =>
                x.Send(Arg<TransportMessage>.Matches(m => 
                    m.Headers[Headers.OriginatingSagaId] == sagaData.Id.ToString() && //id of the current saga
                    //todo: should we really us the AssemblyQualifiedName here? (what if users move sagas btw assemblies
                    m.Headers[Headers.OriginatingSagaType] == typeof(MySaga).AssemblyQualifiedName 
                    ), Arg<Address>.Is.Anything));
        }


        class MySaga : Saga<MySagaData>, IAmStartedByMessages<MessageToProcess>
        {
            public void Handle(MessageToProcess message)
            {
                Bus.Send(new MessageSentFromSaga());
            }
        }

        class MySagaData : ContainSagaData
        {
        }

        class MessageToProcess : IMessage { }

        class MessageSentFromSaga : IMessage{ }

    }


}

