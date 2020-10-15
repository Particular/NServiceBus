using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Sagas;
using NUnit.Framework;

namespace NServiceBus.Core.Tests.Sagas.TypeBasedSagas
{
    [TestFixture]
    public class When_saga_is_correlated_on_a_unsupported_datetimeoffset_property_type
    {
        [Test]
        public void Should_throw()
        {
            var ex = Assert.Throws<Exception>(() => SagaMetadata.Create(typeof(SagaWithNoStartMessage), new List<Type>(), new Conventions()));

            StringAssert.Contains("DateTimeOffset is not supported for correlated properties", ex.Message);
        }

        class SagaWithNoStartMessage : Saga<SagaWithNoStartMessage.MyEntity>, IAmStartedByMessages<Message1>
        {
            public Task Handle(Message1 message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyEntity> mapper)
            {
                mapper.ConfigureMapping<Message1>(m => m.InvalidProp)
                    .ToSaga(s => s.InvalidProp);
            }

            public class MyEntity : ContainSagaData
            {
                public DateTimeOffset InvalidProp { get; set; }
            }
        }

        class Message1 : IMessage
        {
            public DateTimeOffset InvalidProp { get; set; }
        }
    }
}