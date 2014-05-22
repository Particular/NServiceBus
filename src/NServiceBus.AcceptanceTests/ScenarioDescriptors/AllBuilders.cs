namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using AcceptanceTesting.Support;

    public class AllBuilders : ScenarioDescriptor
    {
        public AllBuilders()
        {
            Add(Builders.Autofac);
            Add(Builders.Windsor);
            Add(Builders.Ninject);
            Add(Builders.Spring);
            Add(Builders.StructureMap);
        }
    }
}