namespace NServiceBus.Core.Tests.Sagas
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    [TestFixture]
    public class When_saga_data_is_not_writable
    {
        [Test]
        public void Should_Throw()
        {
            var ex = Assert.Throws<Exception>(() => SagaMetadata.Create(typeof(SagaWithNonPublicData), new List<Type>(), new Conventions()));

            StringAssert.Contains($"'{nameof(SagaDataWithPrivateSetter.InternalProp)}'", ex.Message);
            StringAssert.Contains($"'{nameof(SagaDataWithPrivateSetter.PrivateProp)}'", ex.Message);
            StringAssert.Contains($"'{nameof(SagaDataWithPrivateSetter.ProtectedProp)}'", ex.Message);
        }
        
        class SagaWithNonPublicData : Saga<SagaDataWithPrivateSetter>, IAmStartedByMessages<Message1>
        {
            public Task Handle(Message1 message, IMessageHandlerContext context)
            {
                return TaskEx.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaDataWithPrivateSetter> mapper)
            {
                mapper.ConfigureMapping<Message1>(m => m.MessageProp)
                    .ToSaga(s => s.PublicProp);
            }
        }
        
        public class SagaDataWithPrivateSetter : ContainSagaData
        {
            public int PrivateProp { get; private set; }
            public int InternalProp { get; internal set; }
            public int ProtectedProp { get; internal set; }
            public int PublicProp { get; set; }
            public DateTime GetterOnlyProp { get; } = DateTime.Now;
        }

        class Message1 : IMessage
        {
            public int MessageProp { get; set; }
        }
    }
}