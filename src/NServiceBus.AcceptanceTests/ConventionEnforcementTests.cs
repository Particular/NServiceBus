namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;

    [TestFixture]
    public class ConventionEnforcementTests : NServiceBusAcceptanceTest
    {
        [Test]
        public void Ensure_all_tests_derive_from_a_common_base_class()
        {
            var testTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(HasTestMethod);

            var missingBaseClass = testTypes
                .Where(t => t.BaseType == null || !typeof(NServiceBusAcceptanceTest).IsAssignableFrom(t))
                .ToList();

            CollectionAssert.IsEmpty(missingBaseClass, string.Join(",", missingBaseClass));
        }

        static bool HasTestMethod(Type t)
        {
            return t.GetMethods().Any(m => m.GetCustomAttributes<TestAttribute>().Any());
        }
    }
}