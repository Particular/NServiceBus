namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Linq;
    using Contexts;
    using NServiceBus.Sagas;
    using NServiceBus.Sagas.Finders;
    using NUnit.Framework;
    using Persistence.InMemory.SagaPersister;
    using Saga;

    [TestFixture]
    public class Sagas : using_the_unicastBus
    {
        [Test]
        public void Message_that_starts_saga()
        {
            Features.Sagas.ConfigureSaga(typeof(MySaga));
            var sagaHeaderIdFinder = typeof(HeaderSagaIdFinder<>).MakeGenericType(typeof(MySagaData));

            FuncBuilder.Register(sagaHeaderIdFinder);
            var persister = new InMemorySagaPersister();
            FuncBuilder.Register<ISagaPersister>(()=> persister);

            var receivedMessage = Helpers.Helpers.Serialize(new StartMessage());

            receivedMessage.Headers[Headers.EnclosedMessageTypes] = typeof(StartMessage).FullName;

            RegisterMessageType<StartMessage>();
            RegisterMessageHandlerType<MySaga>();
        
            ReceiveMessage(receivedMessage);

            Assert.AreEqual(1,persister.CurrentSagaEntities().Keys.Count());
            
            //Assert.True(Handler2.Called);
        }

        class MySaga : Saga<MySagaData>,IAmStartedByMessages<StartMessage>
        {
            public void Handle(StartMessage message)
            {
                
            }


        }

        class MySagaData : ContainSagaData
        {
            
        }

        class StartMessage:IMessage
        {
        }
    }
    }

