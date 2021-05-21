namespace NServiceBus.Core.Tests.API.Infra
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    static partial class MethodBaseExtensions
    {
        public static IEnumerable<string> Prettify(this IEnumerable<Type> types) =>
            types
                .OrderBy(type => type.FullName)
                .Select(type => type.FullName);
    }
}
