#pragma warning disable 1591
namespace NServiceBus.PersistenceTests.Sagas
{
    using System;

    public class TestSagaData : ContainSagaData
    {
        public string SomeId { get; set; } = "Test";

        public DateTime DateTimeProperty { get; set; }
    }
}