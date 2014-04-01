namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System.Collections.Generic;
    using System.Linq;
    using AcceptanceTesting.Support;
    using ObjectBuilder.Common;

    public static class Builders
    {
        static IList<RunDescriptor> availableTransports;

        public static IEnumerable<RunDescriptor> AllAvailable
        {
            get { return availableTransports ?? (availableTransports = GetAllAvailable().ToList()); }
        }

        static IEnumerable<RunDescriptor> GetAllAvailable()
        {
            var builders = TypeScanner.GetAllTypesAssignableTo<IContainer>()
                .Where(t => !t.Assembly.FullName.StartsWith("NServiceBus.Core") && //exclude the default builder
                            t.Name.EndsWith("ObjectBuilder") ) //exclude the ninject child container 
                .ToList();

            return from builder in builders let name = builder.Name.Replace("ObjectBuilder", "") select (new RunDescriptor
            {
                Key = name,
                Settings = new Dictionary<string, string> { { "Builder", builder.AssemblyQualifiedName } }
            });
        }

        public static RunDescriptor Autofac
        {
            get { return AllAvailable.SingleOrDefault(r => r.Key == "Autofac"); }
        }

        public static RunDescriptor Ninject
        {
            get { return AllAvailable.SingleOrDefault(r => r.Key == "Ninject"); }
        }

        public static RunDescriptor StructureMap
        {
            get { return AllAvailable.SingleOrDefault(r => r.Key == "StructureMap"); }
        }

        public static RunDescriptor Windsor
        {
            get { return AllAvailable.SingleOrDefault(r => r.Key == "Windsor"); }
        }

        public static RunDescriptor Spring
        {
            get { return AllAvailable.SingleOrDefault(r => r.Key == "Spring"); }
        }

        public static RunDescriptor Unity
        {
            get { return AllAvailable.SingleOrDefault(r => r.Key == "Unity"); }
        }
    }
}