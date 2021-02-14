namespace NServiceBus.Core.Tests.API.Infra
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    static partial class ParameterInfoExtensions
    {
        public static bool ContainsCancellableContext(this IEnumerable<ParameterInfo> parameters) =>
            parameters.Any(parameter => typeof(ICancellableContext).IsAssignableFrom(parameter.ParameterType));

        public static bool ContainsCancellationToken(this IEnumerable<ParameterInfo> parameters) =>
            parameters.Any(parameter => parameter.ParameterType == typeof(CancellationToken));
    }
}
