namespace NServiceBus.Core.Tests.API.Infra
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    static partial class MethodBaseExtensions
    {
        public static bool IsCompilerGenerated(this MethodBase methodBase) =>
            methodBase.GetCustomAttributes(typeof(CompilerGeneratedAttribute)).Any() ||
            methodBase.DeclaringType.GetCustomAttributes(typeof(CompilerGeneratedAttribute)).Any();

        public static bool IsObsolete(this MethodBase methodBase) =>
            methodBase.GetCustomAttributes(typeof(ObsoleteAttribute), true).Any() ||
            ((methodBase as MethodInfo)?.ReturnType.IsObsolete() ?? false) ||
            methodBase.GetParameters().Any(param => param.ParameterType.IsObsolete());

        public static IEnumerable<string> Prettify(this IEnumerable<MethodBase> methodBases) =>
            methodBases
                .OrderBy(_ => _, MethodBaseComparer.Instance)
                .Select(methodBase => methodBase.Prettify());

        public static string Prettify(this MethodBase methodBase) => $"{methodBase.DeclaringType.FullName} {{ {methodBase} }}";
    }
}
