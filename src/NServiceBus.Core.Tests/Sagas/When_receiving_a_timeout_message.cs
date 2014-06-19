namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using Saga;

    [TestFixture]
    class When_receiving_a_timeout_message : with_sagas
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

            Assert.AreEqual(1, persister.CurrentSagaEntities.Count, "Existing saga should be found");
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
            

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
            {
            }
        }

        class MySagaData : ContainSagaData
        {
            public bool TimeoutCalled { get; set; }
        }

        class MyTimeout:IMessage { }
    }
}