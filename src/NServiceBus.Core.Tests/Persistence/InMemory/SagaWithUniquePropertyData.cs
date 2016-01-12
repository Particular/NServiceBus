namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;

    class SagaWithUniqueProperty : Saga<SagaWithUniquePropertyData>, IAmStartedByMessages<StartMessage>
    {
        public Task Handle(StartMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithUniquePropertyData> mapper)
        {
            mapper.ConfigureMapping<M12, string>(m => m.UniqueString).ToSaga(s => s.UniqueString);
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