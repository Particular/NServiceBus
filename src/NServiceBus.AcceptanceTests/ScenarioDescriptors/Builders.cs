namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System.Collections.Generic;
    using System.Linq;
    using AcceptanceTesting.Support;
    using Container;

    public static class Builders
    {
        static IEnumerable<RunDescriptor> GetAllAvailable()
        {
            var builders = TypeScanner.GetAllTypesAssignableTo<ContainerDefinition>()
                .Where(t => !t.Assembly.FullName.StartsWith("NServiceBus.Core"))//exclude the default builder
                .ToList();

            return from builder in builders
                   select (new RunDescriptor
                       {
                           Key = builder.Name,
                           Settings = new Dictionary<string, string> { { "Builder", builder.AssemblyQualifiedName } }
                       });
        }

        public static RunDescriptor Default
        {
            get
            {
                return GetAllAvailable().FirstOrDefault();
            }
        }
    }
}