namespace NServiceBus.Core.Tests.Persistence.InMemory
{
    using System;
    using System.Threading.Tasks;

    class SagaWithoutUniqueProperty : Saga<SagaWithoutUniquePropertyData>, IAmStartedByMessages<SagaWithoutUniquePropertyStartingMessage>
    {
        public Task Handle(SagaWithoutUniquePropertyStartingMessage message, IMessageHandlerContext context)
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

    class SagaWithoutUniquePropertyStartingMessage
    {
        public string NonUniqueString { get; set; }
    }
}