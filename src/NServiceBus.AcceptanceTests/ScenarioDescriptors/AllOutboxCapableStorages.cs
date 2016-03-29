namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System;
    using AcceptanceTesting.Support;
    using NServiceBus.Persistence;

    public class AllOutboxCapableStorages : ScenarioDescriptor
    {
        public AllOutboxCapableStorages()
        {
            var defaultSettings = Persistence.Default;

            var definitionType = defaultSettings.Settings.Get<Type>("Persistence");
            var definition = (PersistenceDefinition) Activator.CreateInstance(definitionType, true);
            if (definition.HasSupportFor<StorageType.Outbox>())
            {
                Add(defaultSettings);
            }
        }
    }
}