namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System.Collections.Generic;
    using System.Linq;
    using AcceptanceTesting.Support;
    using ObjectBuilder.Common;

    public class AllBuilders : ScenarioDescriptor
    {
        public AllBuilders()
        {
            var builders = TypeScanner.GetAllTypesAssignableTo<IContainer>()
                .Where(t => !t.Assembly.FullName.StartsWith("NServiceBus.Core") && //exclude the default builder
                   t.Name.EndsWith("ObjectBuilder") ) //exclude the ninject child container 
                .ToList();

            foreach (var builder in builders)
            {
                var name = builder.Name.Replace("ObjectBuilder", "");
                Add(new RunDescriptor
                {
                    Key = name,
                    Settings = new Dictionary<string, string> { { "Builder", builder.AssemblyQualifiedName } }
                });
            }
        }
    }
}