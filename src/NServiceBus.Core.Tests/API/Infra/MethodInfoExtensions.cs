namespace NServiceBus.Core.Tests.API.Infra
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    static partial class MethodInfoExtensions
    {
        public static bool IsOn(this MethodInfo method, params Type[] types) =>
            types.Any(type =>
                type.IsAssignableFrom(method.DeclaringType) ||
                (method.GetCustomAttributes<ExtensionAttribute>().Any() && type.IsAssignableFrom(method.GetParameters().First().ParameterType)));

        public static bool IsVisible(this MethodInfo method) =>
            method.DeclaringType.IsVisible && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);

        public static bool IsExplicitInterfaceImplementation(this MethodInfo method)
        {
            const MethodAttributes explicitAttributes =
                MethodAttributes.PrivateScope | MethodAttributes.Private | MethodAttributes.Final |
                MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.VtableLayoutMask;

            return !method.IsStatic && method.Attributes == explicitAttributes;
        }
    }
}
