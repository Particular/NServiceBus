namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;

    class AnotherSagaTwoUniqueProperty : Saga<AnotherSagaWithUniquePropertyData>, IAmStartedByMessages<M1>
    {
        public Task Handle(M1 message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AnotherSagaWithUniquePropertyData> mapper)
        {
            mapper.ConfigureMapping<M1, string>(m => m.UniqueString).ToSaga(s => s.UniqueString);
        }
    }

    class M1
    {
        public string UniqueString { get; set; }
    }

    public class AnotherSagaWithUniquePropertyData : ContainSagaData
    {
        public string UniqueString { get; set; }
    }
}