namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Hosting.Helpers;

    public class TypeScanner
    {
        static IEnumerable<Assembly> AvailableAssemblies
        {
            get
            {
                if (assemblies == null)
                {
                    var result = new AssemblyScanner().GetScannableAssemblies();

                    assemblies = result.Assemblies.Where(a =>
                    {
                        var references = a.GetReferencedAssemblies();

                        return references.All(an => an.Name != "nunit.framework");
                    }).ToList();
                }

                return assemblies;
            }
        }

        public static IEnumerable<Type> GetAllTypesAssignableTo<T>()
        {
            return AvailableAssemblies.SelectMany(a => a.GetTypes())
                .Where(t => typeof(T).IsAssignableFrom(t) && t != typeof(T))
                .ToList();
        }

        static List<Assembly> assemblies;
    }
}