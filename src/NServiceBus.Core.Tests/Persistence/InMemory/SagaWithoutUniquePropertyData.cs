namespace NServiceBus.Core.Tests.Persistence.InMemory
{
    using System;
    using System.Threading.Tasks;

    class SagaWithoutUniqueProperty : Saga<SagaWithoutUniquePropertyData>, IAmStartedByMessages<M13>
    {
        public Task Handle(M13 message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithoutUniquePropertyData> mapper)
        {
            //not implemented
        }
    }
    public class SagaWithoutUniquePropertyData : ContainSagaData
    {
        public string NonUniqueString { get; set; }
    }

    class M13
    {
        public string NonUniqueString { get; set; }
    }
}