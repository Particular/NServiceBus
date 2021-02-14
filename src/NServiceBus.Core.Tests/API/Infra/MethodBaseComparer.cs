namespace NServiceBus.Core.Tests.API.Infra
{
    using System.Collections.Generic;
    using System.Reflection;

    class MethodBaseComparer : Comparer<MethodBase>
    {
        public static MethodBaseComparer Instance { get; } = new MethodBaseComparer();

        public override int Compare(MethodBase x, MethodBase y) =>
            Print(x).CompareTo(Print(y));

        static string Print(MethodBase methodBase) =>
            $"{methodBase.DeclaringType.Namespace}.{methodBase.DeclaringType.Name}.{methodBase.Name}.{methodBase}";
    }
}
