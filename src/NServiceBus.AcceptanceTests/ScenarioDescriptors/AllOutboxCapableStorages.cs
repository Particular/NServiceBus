namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System;
    using AcceptanceTesting.Support;
    using NServiceBus.Persistence;

    public class AllOutboxCapableStorages:ScenarioDescriptor
    {
        public AllOutboxCapableStorages()
        {
            var defaultStorage = Persistence.Default;

            var definitionType = Type.GetType(defaultStorage.Settings["Persistence"]);

            var definition = (PersistenceDefinition)Activator.CreateInstance(definitionType, true);
#pragma warning disable 618
            if (definition.HasSupportFor(Storage.Outbox))
#pragma warning restore 618
            {
                Add(defaultStorage);
            }
        }
    }
}