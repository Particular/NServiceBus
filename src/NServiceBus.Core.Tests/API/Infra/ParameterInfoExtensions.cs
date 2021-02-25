namespace NServiceBus.Core.Tests.API.Infra
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    static partial class ParameterInfoExtensions
    {
        public static IEnumerable<ParameterInfo> CancellableContexts(this IEnumerable<ParameterInfo> parameters) =>
            parameters.Where(parameter => typeof(ICancellableContext).IsAssignableFrom(parameter.ParameterType));

        public static IEnumerable<ParameterInfo> CancellationTokens(this IEnumerable<ParameterInfo> parameters) =>
            parameters.Where(parameter => parameter.ParameterType.IsCancellationToken());

        public static bool IsExplicitlyNamed(this ParameterInfo parameter) =>
            parameter.Name.Length > 17 && parameter.Name.EndsWith("CancellationToken");
    }
}
