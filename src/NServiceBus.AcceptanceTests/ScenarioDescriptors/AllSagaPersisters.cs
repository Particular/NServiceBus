namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using AcceptanceTesting.Support;

    public class AllSagaPersisters : ScenarioDescriptor
    {
        public AllSagaPersisters()
        {
            Add(SagaPersisters.InMemory);
            Add(SagaPersisters.Raven);
            Add(SagaPersisters.NHibernate);
        }
    }
}