namespace NServiceBus.Hosting.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    internal static class AssemblyListExtensions
    {
        public static IEnumerable<Type> AllTypes(this IEnumerable<Assembly> assemblies)
        {
            return assemblies.SelectMany(assembly => assembly.GetTypes());
        }

        public static IEnumerable<Type> AllTypesAssignableTo<T>(this IEnumerable<Assembly> assemblies)
        {
            var type = typeof(T);
            return assemblies.Where(type.Assembly.IsReferencedByOrEquals)
                .AllTypes()
                .Where(t => t != type && type.IsAssignableFrom(t));
        }

        public static IEnumerable<Type> WhereConcrete(this IEnumerable<Type> types)
        {
            return types.Where(x => !x.IsInterface && !x.IsAbstract);
        }

        static bool IsReferencedByOrEquals(this Assembly referenceAssembly, Assembly targetAssembly)
        {
            var name = referenceAssembly.GetName().Name;
            return targetAssembly.GetName().Name == name || targetAssembly.GetReferencedAssemblies().Any(y => y.Name == name);
        }
    }
}