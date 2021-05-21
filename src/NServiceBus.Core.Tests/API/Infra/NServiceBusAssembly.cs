namespace NServiceBus.Core.Tests.API.Infra
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    static class NServiceBusAssembly
    {
        public static readonly List<Type> Types = typeof(IMessage).Assembly.GetTypes()
            .Where(type => !type.IsObsolete())
            .ToList();
    }
}
