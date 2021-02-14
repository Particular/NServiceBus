namespace NServiceBus.Core.Tests.API.Infra
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    static class NServiceBusAssembly
    {
        public static readonly List<MethodInfo> Methods = typeof(IMessage).Assembly.GetTypes()
            .Where(type => !type.IsObsolete())
            .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(method => !typeof(Delegate).IsAssignableFrom(method.DeclaringType) || method.Name == "Invoke")
            .Where(method => !method.IsCompilerGenerated())
            .Where(method => !method.IsObsolete())
            .ToList();
    }
}
