namespace NServiceBus.Core.Tests.API.Infra
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    static partial class MethodInfoExtensions
    {
        public static bool IsCompilerGenerated(this MethodInfo method) =>
            method.GetCustomAttributes(typeof(CompilerGeneratedAttribute)).Any() ||
            method.DeclaringType.GetCustomAttributes(typeof(CompilerGeneratedAttribute)).Any();

        public static bool IsObsolete(this MethodInfo method) =>
            method.GetCustomAttributes(typeof(ObsoleteAttribute), true).Any() ||
            method.ReturnType.IsObsolete() ||
            method.GetParameters().Any(param => param.ParameterType.IsObsolete());

        public static bool IsOn(this MethodInfo method, params Type[] types) =>
            types.Any(type =>
                type.IsAssignableFrom(method.DeclaringType) ||
                (method.GetCustomAttributes<ExtensionAttribute>().Any() && type.IsAssignableFrom(method.GetParameters().First().ParameterType)));

        public static bool IsVisible(this MethodInfo method) =>
            method.DeclaringType.IsVisible && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);

        public static bool HasCancellableContext(this MethodInfo method) =>
            method.GetParameters().Any(parameter => typeof(ICancellableContext).IsAssignableFrom(parameter.ParameterType));

        public static IEnumerable<string> Prettify(this IEnumerable<MethodInfo> methods) =>
            methods
                .OrderBy(_ => _, MethodInfoComparer.Instance)
                .Select(method => method.Prettify());

        public static string Prettify(this MethodInfo method) => $"{method.DeclaringType.FullName} {{ {method} }}";
    }
}
