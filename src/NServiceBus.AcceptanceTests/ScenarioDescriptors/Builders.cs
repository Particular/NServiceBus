namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AcceptanceTesting.Support;
    using Container;

    public static class Builders
    {
        public static RunDescriptor Default => GetAllAvailable().FirstOrDefault();

        static IEnumerable<RunDescriptor> GetAllAvailable()
        {
            foreach (var builder in foundDefinitions.Value)
            {
                var descriptor = new RunDescriptor(builder.Name);
                descriptor.Settings.Set("Builder", builder);
                yield return descriptor;
            }
        }

        static Lazy<List<Type>> foundDefinitions = new Lazy<List<Type>>(() =>
        {
            return TypeScanner.GetAllTypesAssignableTo<ContainerDefinition>()
                .Where(t => !t.Assembly.FullName.StartsWith("NServiceBus.Core")) //exclude the default builder
                .ToList();
        });
    }
}