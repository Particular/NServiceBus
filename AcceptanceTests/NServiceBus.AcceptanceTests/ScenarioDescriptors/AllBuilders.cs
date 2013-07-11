namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using NServiceBus.AcceptanceTesting.Support;

    public class AllBuilders:ScenarioDescriptor
    {
        public AllBuilders()
        {
            Add(Builders.Unity);
            Add(Builders.Autofac);
            Add(Builders.Windsor);
            Add(Builders.Spring);
            Add(Builders.Ninject);
            Add(Builders.StructureMap);

        }
    }
}