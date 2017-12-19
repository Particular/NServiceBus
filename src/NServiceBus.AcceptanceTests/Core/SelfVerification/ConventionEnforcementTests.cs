namespace NServiceBus.AcceptanceTests.Core.SelfVerification
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

        [Test]
        public void Ensure_all_messages_are_public()
        {
            var testTypes = Assembly.GetExecutingAssembly().GetTypes();

            var missingBaseClass = testTypes
                .Where(t => !t.IsPublic && !t.IsNestedPublic)
                .Where(t =>
                        typeof(ICommand).IsAssignableFrom(t) ||
                        typeof(IMessage).IsAssignableFrom(t) ||
                        typeof(IEvent).IsAssignableFrom(t)
                )
                .ToList();

            CollectionAssert.IsEmpty(missingBaseClass, string.Join(",", missingBaseClass));
        }

        [Test]
        public void Ensure_all_sagadatas_are_public()
        {
            var testTypes = Assembly.GetExecutingAssembly().GetTypes();

            var sagaDatas = testTypes
                .Where(t => !t.IsPublic && !t.IsNestedPublic)
                .Where(t => typeof(IContainSagaData).IsAssignableFrom(t))
                .ToList();

            CollectionAssert.IsEmpty(sagaDatas, string.Join(",", sagaDatas));
        }

        static bool HasTestMethod(Type t)
        {
            return t.GetMethods().Any(m => m.GetCustomAttributes<TestAttribute>().Any());
        }
    }
}