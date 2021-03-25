namespace NServiceBus.Core.Tests.API
{
    using System;
    using System.Linq;
    using NServiceBus.Core.Tests.API.Infra;
    using NUnit.Framework;

    static class Types
    {
        [Test]
        public static void StaticTypesDoNotHaveInterfacePrefixes()
        {
            var violators = NServiceBusAssembly.Types
                .Where(type => type.IsStatic())
                .Where(type => type.Name.Length > 1)
                .Where(type => type.Name.StartsWith("I", StringComparison.Ordinal))
                .Where(type => char.IsUpper(type.Name.ElementAt(1)))
                .Prettify()
                .ToList();

            Console.Error.WriteViolators(violators);

            Assert.IsEmpty(violators);
        }
    }
}
