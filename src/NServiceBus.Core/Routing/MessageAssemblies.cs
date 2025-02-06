namespace NServiceBus;

using System.Collections.Generic;
using System.Reflection;

partial class RoutingComponent
{
    internal class MessageAssemblies
    {
        readonly HashSet<Assembly> assemblies = [];

        public void Add(Assembly assembly)
            => assemblies.Add(assembly);

        public Assembly[] GetAll() => [.. assemblies];
    }
}