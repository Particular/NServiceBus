namespace NServiceBus.Persistence.ComponentTests
{
    using System.Collections.Generic;

    public class SagaWithComplexTypeEntity : ContainSagaData
    {
        public string CorrelationProperty { get; set; }
        public List<int> Ints { get; set; }
    }
}