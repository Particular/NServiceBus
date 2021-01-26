namespace NServiceBus.Core.Tests.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NServiceBus.Core.Tests.API.Infra;
    using NUnit.Framework;

    static class CancellationTokenParameters
    {
        static readonly List<MethodBase> methodsAndConstructors = NServiceBusAssembly.MethodsAndConstructors
            .Where(methodBase => methodBase.GetParameters().CancellationTokens().Any())
            .ToList();

        [Test]
        public static void AreUniquePerSignature()
        {
            var violators = methodsAndConstructors
                .Where(methodBase => methodBase.GetParameters().CancellationTokens().Count() > 1)
                .Prettify()
                .ToList();

            Console.Error.WriteViolators(violators);

            Assert.IsEmpty(violators);
        }

        [Test]
        public static void AreLast()
        {
            var violators = methodsAndConstructors
                .Where(methodBase => !methodBase.GetParameters().Last().ParameterType.IsCancellationToken())
                .Prettify()
                .ToList();

            Console.Error.WriteViolators(violators);

            Assert.IsEmpty(violators);
        }

        [Test]
        public static void AreNamedCancellationToken()
        {
            var violators = methodsAndConstructors
                .Where(methodBase => methodBase.GetParameters().CancellationTokens().Any(param =>
                    !param.Name.Equals("cancellationToken", StringComparison.Ordinal) &&
                    !param.Name.EndsWith("CancellationToken", StringComparison.Ordinal))) // e.g. stopRequestedCancellationToken
                .Prettify()
                .ToList();

            Console.Error.WriteViolators(violators);

            Assert.IsEmpty(violators);
        }
    }
}
