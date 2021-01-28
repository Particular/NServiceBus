namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.Linq;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    [TestFixture]
    public static class EnsureNativeBehvaviors
    {
        static readonly Type[] abstractTypesForExternalUseOnly = new[]
        {
            typeof(Behavior<>),
            typeof(ForkConnector<,>),
        };

        [Test]
        public static void CoreBehaviorsMustNotUseAbstractClasses()
        {
            var violators = typeof(IBehavior).Assembly.GetTypes()
                .Where(UsesAbstractClass)
                .ToList();

            Console.Error.WriteLine($"Violators of {nameof(CoreBehaviorsMustNotUseAbstractClasses)}:{Environment.NewLine}{string.Join(Environment.NewLine, violators)}");

            Assert.IsEmpty(violators, $"For performance reasons, built-in behaviors are not allowed to inherit from abstract behavior classes. Implement IBehavior<Tin, TOut> directly, using the same context type for both TIn and TOut.");
        }

        static bool UsesAbstractClass(Type type)
        {
            if (type == null || abstractTypesForExternalUseOnly.Contains(type))
            {
                return false;
            }

            if (type.IsGenericType && abstractTypesForExternalUseOnly.Contains(type.GetGenericTypeDefinition()))
            {
                return true;
            }

            return UsesAbstractClass(type.BaseType);
        }
    }
}