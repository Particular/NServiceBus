namespace NServiceBus.Core.Tests.API.Infra
{
    using System.Collections.Generic;
    using System.Reflection;

    class MethodInfoComparer : Comparer<MethodInfo>
    {
        public static MethodInfoComparer Instance { get; } = new MethodInfoComparer();

        public override int Compare(MethodInfo x, MethodInfo y) =>
            Print(x).CompareTo(Print(y));

        static string Print(MethodInfo methodInfo) =>
            $"{methodInfo.DeclaringType.Namespace}.{methodInfo.DeclaringType.Name}.{methodInfo.Name}.{methodInfo}";
    }
}
