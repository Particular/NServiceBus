namespace NServiceBus.Core.Tests.Sagas.TypeBasedSagas;

using System;
using System.Threading.Tasks;
using NServiceBus.Sagas;
using NUnit.Framework;

[TestFixture]
public class When_saga_has_duplicate_not_found_handlers
{
    [Test]
    public void Should_throw()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => SagaMetadata.Create(typeof(SagaWithDuplicateNotFoundHandlers)));

        Assert.That(ex.Message, Does.Contain("Saga not found handler already configured"));
    }

    class SagaWithDuplicateNotFoundHandlers : Saga<SagaWithDuplicateNotFoundHandlers.MyEntity>, IHandleMessages<Message1>
    {
        public Task Handle(Message1 message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyEntity> mapper)
        {
            mapper.ConfigureNotFoundHandler<MyNotFoundHandler>();
            mapper.ConfigureNotFoundHandler<MyNotFoundHandler>();
        }

        class MyNotFoundHandler : NServiceBus.IHandleSagaNotFound
        {
            public Task Handle(object message, IMessageProcessingContext context) => throw new NotImplementedException();
        }

        public class MyEntity : ContainSagaData;
    }

    class Message1 : IMessage;
}