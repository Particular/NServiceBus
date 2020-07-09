namespace NServiceBus.PersistenceTests.Sagas
{
    using System;

    public class SagaWithoutCorrelationPropertyData : ContainSagaData
    {
        public string FoundByFinderProperty { get; set; }

        public DateTime DateTimeProperty { get; set; }
    }
}