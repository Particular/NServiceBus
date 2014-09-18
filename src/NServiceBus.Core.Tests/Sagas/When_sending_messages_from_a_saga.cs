namespace NServiceBus.Unicast.Tests
{
    using System.Linq;
    using NUnit.Framework;
    using Rhino.Mocks;
    using Saga;

    [TestFixture]
    class When_sending_messages_from_a_saga : with_sagas
    {
        [Test]
        public void Should_attach_the_originating_saga_id_as_a_header()
        {
            RegisterMessageType<MessageSentFromSaga>();
            RegisterSaga<MySaga>();

            ReceiveMessage(new MessageToProcess(), mapper: MessageMapper);

            var sagaData = (MySagaData)persister.CurrentSagaEntities.First().Value.SagaEntity;

            messageSender.AssertWasCalled(x =>
                x.Send(Arg<TransportMessage>.Matches(m => 
                    m.Headers[Headers.OriginatingSagaId] == sagaData.Id.ToString() && //id of the current saga
                    //todo: should we really us the AssemblyQualifiedName here? (what if users move sagas btw assemblies
                    m.Headers[Headers.OriginatingSagaType] == typeof(MySaga).AssemblyQualifiedName 
                    ), Arg<SendOptions>.Is.Anything));
        }


        class MySaga : Saga<MySagaData>, IAmStartedByMessages<MessageToProcess>
        {
            public void Handle(MessageToProcess message)
            {
                Bus.Send(new MessageSentFromSaga());
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
            {
            }
        }

        class MySagaData : ContainSagaData
        {
        }

        class MessageToProcess : IMessage { }

        class MessageSentFromSaga : IMessage{ }

    }
}