namespace NServiceBus.Core.Tests.API.Infra
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    static class NServiceBusAssembly
    {
        public static readonly List<Type> Types = typeof(IMessage).Assembly.GetTypes()
            .Where(type => !type.IsObsolete())
            .ToList();

        public static readonly List<MethodInfo> Methods = Types
            .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(method => !typeof(Delegate).IsAssignableFrom(method.DeclaringType) || method.Name == "Invoke")
            .Where(method => !method.IsCompilerGenerated())
            .Where(method => !method.IsObsolete())
            .ToList();

        public static readonly List<ConstructorInfo> Constructors = Types
            .SelectMany(type => type.GetConstructors(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(method => !method.IsCompilerGenerated())
            .Where(method => !method.IsObsolete())
            .ToList();

        public static readonly List<MethodBase> MethodsAndConstructors = Methods
            .Concat(Constructors.Cast<MethodBase>())
            .ToList();
    }
}
