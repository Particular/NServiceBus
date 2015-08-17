namespace NServiceBus.AcceptanceTests.SelfVerification
{
    using System;
    using System.Linq;
    using System.Reflection;
    using NServiceBus.Saga;
    using NUnit.Framework;

    [TestFixture]
    public class When_running_acceptance_tests
    {
        [Test]
        public void All_saga_entities_in_acceptance_tests_should_have_virtual_properties()
        {
            // Because otherwise NHibernate gets cranky!
            var sagaEntities = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(IContainSagaData).IsAssignableFrom(t) && !t.IsInterface)
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
    }
}
