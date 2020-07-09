namespace NServiceBus.PersistenceTests.Sagas
{
    using System;

    public class SagaWithCorrelationPropertyData : ContainSagaData
    {
        public string CorrelatedProperty { get; set; }

        public DateTime DateTimeProperty { get; set; }
    }
}