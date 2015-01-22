namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using NUnit.Framework;
    using UnicastBus = NServiceBus.Unicast.UnicastBus;

    [TestFixture]
    public class StandardsTests
    {
        [Test]
        public void VerifyFeatureNaming()
        {
            foreach (var featureType in GetFeatures())
            {
                Assert.AreEqual("NServiceBus.Features", featureType.Namespace, "Features should be in the NServiceBus.Features namespace. " + featureType.FullName);
                Assert.IsFalse(featureType.Name.EndsWith("Feature"), "Features should not be suffixed with 'Feature'. " + featureType.FullName);
                if (featureType.IsPublic)
                {
                    var constructorInfo = featureType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new Type[]
                    {
                    }, null);
                    Assert.IsFalse(constructorInfo.IsPublic, "Features should have an internal constructor. " + featureType.FullName);
                }
            }
        }

        [Test]
        public void LoggersShouldBeStaticField()
        {
            foreach (var type in typeof(UnicastBus).Assembly.GetTypes())
            {
                foreach (var field in type.GetFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public))
                {
                    if (field.FieldType == typeof(ILog))
                    {
                        Assert.IsTrue(field.IsStatic, "Logger fields should be static " + type.FullName);
                    }
                }
            }
        }

        [Test]
        public void VerifyBehaviorNaming()
        {
            foreach (var featureType in GetBehaviors())
            {
                Assert.IsFalse(featureType.IsPublic, "Behaviors should internal " + featureType.FullName);
                Assert.AreEqual("NServiceBus", featureType.Namespace, "Behaviors should be in the NServiceBus namespace since it reduces the 'wall of text' problem when looking at pipeline stack traces. " + featureType.FullName);
                Assert.IsTrue(featureType.Name.EndsWith("Behavior") || featureType.Name.EndsWith("Connector"), "Behaviors should be suffixed with 'Behavior' or 'Connector'. " + featureType.FullName);
            }
        }

        [Test]
        public void VerifyAttributesAreSealed()
        {
            foreach (var attributeType in GetAttributeTypes())
            {
                Assert.IsTrue(attributeType.IsSealed, attributeType.FullName + " should be sealed.");
            }
        }

        static IEnumerable<Type> GetBehaviors()
        {
            return typeof(UnicastBus).Assembly.GetTypes()
                .Where(type => type.GetInterfaces().Any(face=>face.Name.StartsWith("IBehavior")) && !type.IsAbstract &&!type.IsGenericType);
        }
        static IEnumerable<Type> GetFeatures()
        {
            return typeof(UnicastBus).Assembly.GetTypes()
                .Where(type => typeof(Feature).IsAssignableFrom(type) && !type.IsAbstract);
        }

        static IEnumerable<Type> GetAttributeTypes()
        {
            return typeof(UnicastBus).Assembly.GetTypes()
                .Where(type => type.Namespace != null &&
                               typeof(Attribute).IsAssignableFrom(type) &&
                               //Ignore log4net attributes
                               !type.Namespace.Contains("log4net") &&
                               //Ignore Newtonsoft attributes
                               !type.Namespace.Contains("Newtonsoft") &&
                               //TODO: remove when gitversion is updated
                               !type.Name.EndsWith("ReleaseDateAttribute") &&
                               !type.Name.EndsWith("NugetVersionAttribute"));
        }
    }
}