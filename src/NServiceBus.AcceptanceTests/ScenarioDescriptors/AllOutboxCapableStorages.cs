namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System;
    using AcceptanceTesting.Support;
    using NServiceBus.Persistence;

    public class AllOutboxCapableStorages:ScenarioDescriptor
    {
        public AllOutboxCapableStorages()
        {
            var defaultStorage = ScenarioDescriptors.Persistence.Default;

            var definitionType = Type.GetType(defaultStorage.Settings["Persistence"]);

            var definition = (PersistenceDefinition) Activator.CreateInstance(definitionType);

            if (definition.HasOutboxStorage)
            {
                Add(defaultStorage);
            }
        }
    }
}