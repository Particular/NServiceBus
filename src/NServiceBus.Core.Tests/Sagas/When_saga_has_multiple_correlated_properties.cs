namespace NServiceBus.Core.Tests.Sagas.TypeBasedSagas
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Particular.Approvals;

    [TestFixture]
    public class When_saga_has_multiple_correlated_properties
    {
        [Test]
        public void Should_throw()
        {
            var exception = Assert.Throws<Exception>(() => SagaMetadata.Create(typeof(SagaWithMultipleCorrelatedProperties), new List<Type>(), new Conventions()));
            Approver.Verify(exception.Message);
        }

        class SagaWithMultipleCorrelatedProperties : Saga<SagaWithMultipleCorrelatedProperties.MyEntity>,
            IAmStartedByMessages<Message1>,
            IAmStartedByMessages<Message2>
        {
            public Task Handle(Message1 message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
            public Task Handle(Message2 message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyEntity> mapper)
            {
                mapper.ConfigureMapping<Message1>(m=>m.OrderId)
                    .ToSaga(s=>s.OrderId);
                mapper.ConfigureMapping<Message2>(m => m.LegacyOrderId)
                    .ToSaga(s => s.LegacyOrderId);
                mapper.ConfigureMapping<Message1>(m => m.Property2)
                    .ToSaga(s => s.OrderId);
            }

            public class MyEntity : ContainSagaData
            {
                public string OrderId { get; set; }
                public string LegacyOrderId { get; set; }
            }
        }

        class Message1 : IMessage
        {
            public string OrderId { get; set; }
            public string Property2 { get; set; }
        }

        class Message2 : IMessage
        {
            public string LegacyOrderId { get; set; }
        }

    }
}