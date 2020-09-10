namespace NServiceBus.Core.Tests.Sagas.TypeBasedSagas
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    [TestFixture]
    public class When_saga_has_no_start_message
    {
        [Test]
        public void Should_throw()
        {
            var ex = Assert.Throws<Exception>(() => SagaMetadata.Create(typeof(SagaWithNoStartMessage), new List<Type>(), new Conventions()));

            StringAssert.Contains("Sagas must have at least one message that is allowed to start the saga", ex.Message);
        }

        class SagaWithNoStartMessage : Saga<SagaWithNoStartMessage.MyEntity>, IHandleMessages<Message1>
        {
            public Task Handle(Message1 message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyEntity> mapper)
            {
            }

            public class MyEntity : ContainSagaData
            {
            }
        }

        class Message1 : IMessage
        {
        }
    }
}