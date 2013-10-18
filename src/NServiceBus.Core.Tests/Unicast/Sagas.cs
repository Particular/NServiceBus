namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using NUnit.Framework;
    using Saga;

    [TestFixture]
    public class Sagas : using_the_unicastBus
    {
        [Test]
        public void Message_that_starts_saga()
        {
            var receivedMessage = Helpers.Helpers.Serialize(new StartMessage());

            receivedMessage.Headers[Headers.EnclosedMessageTypes] = typeof(StartMessage).FullName;

            RegisterMessageType<StartMessage>();
            RegisterMessageHandlerType<MySaga>();
        
            ReceiveMessage(receivedMessage);


            
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

        class StartMessage
        {
        }
    }
    }

