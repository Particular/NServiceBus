namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System;
    using System.Linq;
    using AcceptanceTesting.Support;
    using Outbox;

    public class OutboxPersisters : ScenarioDescriptor
    {
        public static Type Default
        {
            get
            {
                var persister = TypeScanner.GetAllTypesAssignableTo<IOutboxStorage>()
                    .FirstOrDefault(t => !t.IsInterface && t.Name != "InMemoryOutboxStorage");

                return persister;
            }
        }
    }
}