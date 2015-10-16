namespace NServiceBus.AcceptanceTests.SelfVerification
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;

    [TestFixture]
    public class When_running_saga_tests
    {
        [Test]
        public void All_saga_entities_in_acceptance_tests_should_have_virtual_properties()
        {
            // Because otherwise NHibernate gets cranky!
            var sagaEntities = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(IContainSagaData).IsAssignableFrom(t) && !t.IsInterface &&
                //only include core tests
                t.Namespace != null && t.Namespace.StartsWith("NServiceBus.AcceptanceTests"))
                .ToArray();

            var offenders = 0;

            foreach (var entity in sagaEntities)
            {
                foreach (var property in entity.GetProperties())
                {
                    if (property.GetGetMethod().IsVirtual)
                    {
                        Console.WriteLine("OK: {0}.{1}", entity.FullName, property.Name);
                    }
                    else
                    {
                        offenders++;
                        Console.WriteLine("ERROR: {0}.{1} must be marked as virtual for NHibernate tests to succeed.", entity.FullName, property.Name);
                    }
                }
            }

            Assert.AreEqual(0, offenders);
        }

        [Test]
        public void All_sagas_and_entities_should_have_unique_names()
        {
            var allTypes = Assembly.GetExecutingAssembly().GetTypes();

            var sagas = allTypes.Where(t => typeof(Saga).IsAssignableFrom(t)).ToArray();
            var sagaEntities = allTypes.Where(t => typeof(IContainSagaData).IsAssignableFrom(t) && !t.IsInterface)
               .ToArray();

            var nestedSagaEntityParents = sagaEntities
                .Where(t => t.DeclaringType != null)
                .Select(t => t.DeclaringType)
                .ToArray();

            var usedNames = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            var offenders = 0;

            Console.WriteLine("Sagas / Saga Entities with non-unique names:");
            foreach (var cls in sagas.Union(sagaEntities).Union(nestedSagaEntityParents))
            {
                if (usedNames.Contains(cls.Name))
                {
                    offenders++;
                    Console.WriteLine(cls.FullName);
                }
                usedNames.Add(cls.Name);
            }

            Assert.AreEqual(0, offenders);
        }
    }
}
