namespace NServiceBus.Core.Tests.API
{
    using System;
    using System.Linq;
    using System.Reflection;
    using NServiceBus.Core.Tests.API.Infra;
    using NUnit.Framework;

    static class MethodsAndConstructors
    {
        [Test]
        public static void DoNotHaveCancellationTokensAndCancellableContexts()
        {
            var violators = NServiceBusAssembly.MethodsAndConstructors
                .Where(methodBase => !(methodBase.DeclaringType == typeof(BehaviorContext) && methodBase is ConstructorInfo))
                .Where(methodBase => methodBase.GetParameters().CancellationTokens().Any() && methodBase.GetParameters().CancellableContexts().Any())
                .Prettify()
                .ToList();

            Console.Error.WriteViolators(violators);

            Assert.IsEmpty(violators);
        }
    }
}
