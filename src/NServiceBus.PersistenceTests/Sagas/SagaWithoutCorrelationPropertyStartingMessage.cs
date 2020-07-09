namespace NServiceBus.PersistenceTests.Sagas
{
    public class SagaWithoutCorrelationPropertyStartingMessage : IMessage
    {
        public string FoundByFinderProperty { get; set; }
    }
}