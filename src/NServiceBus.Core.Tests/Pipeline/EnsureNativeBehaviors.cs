namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.Linq;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    [TestFixture]
    public static class EnsureNativeBehvaiors
    {
        [Test]
        public static void CoreBehaviorsMustNotUseAbstractClass()
        {
            var violators = typeof(IBehavior).Assembly.GetTypes()
                .Where(UsesAbstractClass)
                .ToList();

            Console.Error.WriteLine($"Violators of {nameof(CoreBehaviorsMustNotUseAbstractClass)}:{Environment.NewLine}{string.Join(Environment.NewLine, violators)}");

            Assert.IsEmpty(violators, $"For performance reasons, built-in behaviors are not allowed to inherit from abstract class Behavior<T>. Implement IBehavior<Tin, TOut> directly, using the same context type for both TIn and TOut.");
        }

        static bool UsesAbstractClass(Type type) =>
            type != null && type != typeof(Behavior<>) && ((type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Behavior<>)) || UsesAbstractClass(type.BaseType));
    }
}
