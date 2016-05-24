namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;

    class SagaWithUniqueProperty : Saga<SagaWithUniquePropertyData>, IAmStartedByMessages<M12>
    {
        public Task Handle(M12 message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithUniquePropertyData> mapper)
        {
            mapper.ConfigureMapping<M12>(m => m.UniqueString).ToSaga(s => s.UniqueString);
        }
    }
    public class SagaWithUniquePropertyData : ContainSagaData
    {
        public string UniqueString { get; set; }
    }

    class M12
    {
        public string UniqueString { get; set; }
    }
}