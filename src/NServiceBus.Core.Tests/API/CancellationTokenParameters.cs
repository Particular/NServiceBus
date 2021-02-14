namespace NServiceBus.Core.Tests.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using NServiceBus.Core.Tests.API.Infra;
    using NUnit.Framework;

    static class CancellationTokenParameters
    {
        static readonly List<MethodInfo> methods = NServiceBusAssembly.Methods
            .Where(method => method.GetParameters().Any(param => param.ParameterType == typeof(CancellationToken)))
            .ToList();

        [Test]
        public static void AreUniquePerMethod()
        {
            var violators = methods
                .Where(method => method.GetParameters().Count(param => param.ParameterType == typeof(CancellationToken)) > 1)
                .Prettify()
                .ToList();

            Console.Error.WriteViolators(violators);

            Assert.IsEmpty(violators);
        }

        [Test]
        public static void AreLast()
        {
            var violators = methods
                .Where(method => method.GetParameters().Last().ParameterType != typeof(CancellationToken))
                .Prettify()
                .ToList();

            Console.Error.WriteViolators(violators);

            Assert.IsEmpty(violators);
        }

        [Test]
        public static void AreNamedCancellationToken()
        {
            var violators = methods
                .Where(method => method.GetParameters().Any(param =>
                    param.ParameterType == typeof(CancellationToken) &&
                    !param.Name.Equals("cancellationToken", StringComparison.Ordinal) &&
                    !param.Name.EndsWith("CancellationToken", StringComparison.Ordinal))) // e.g. stopRequestedCancellationToken
                .Prettify()
                .ToList();

            Console.Error.WriteViolators(violators);

            Assert.IsEmpty(violators);
        }
    }
}
