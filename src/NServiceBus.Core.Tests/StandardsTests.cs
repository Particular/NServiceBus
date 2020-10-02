namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

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
        public void NonPublicShouldHaveSimpleNamespace()
        {
            // we still need an NServiceBus prefix for people who do logging filtering
            var types = typeof(Endpoint).Assembly.GetTypes()
                .Where(x =>
                    !x.IsPublic &&
                    !x.IsNested &&
                    !IsCompilerGenerated(x) &&
                    !x.FullName.Contains("System") &&
                    !x.FullName.Contains("JetBrains") &&
                    !x.FullName.StartsWith("Newtonsoft.Json") &&
                    !x.FullName.StartsWith("LightInject") &&
                    !x.FullName.StartsWith("SimpleJson") &&
                    !x.FullName.StartsWith("FastExpressionCompiler") &&
                    x.Name != "GitVersionInformation" &&
                    x.Namespace != "Particular.Licensing" &&
                    x.Namespace != "NServiceBus.Features" &&
                    x.Name != "NServiceBusCore_ProcessedByFody" &&
                    x.Namespace != "NServiceBus" &&
                    x.Namespace != "MicrosoftExtensionsDependencyInjection").ToList();
            if (types.Count > 0)
            {
                Assert.IsEmpty(types, $"Non-public types should have 'NServiceBus' namespace{Environment.NewLine}{string.Join(Environment.NewLine, types.Select(x => x.FullName))}");
            }
        }

        static bool IsCompilerGenerated(Type x)
        {
            return Attribute.IsDefined(x, typeof(CompilerGeneratedAttribute), false);
        }

        [Test]
        public void LoggersShouldBeStaticField()
        {
            foreach (var type in typeof(Endpoint).Assembly.GetTypes())
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
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
                Assert.IsTrue(featureType.Name.EndsWith("Terminator") || featureType.Name.EndsWith("Behavior") || featureType.Name.EndsWith("Connector"), "Behaviors should be suffixed with 'Behavior' or 'Connector'. " + featureType.FullName);
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
            return typeof(Endpoint).Assembly.GetTypes()
                .Where(type => type.GetInterfaces().Any(face => face.Name == typeof(IBehavior).Name) && !type.IsAbstract && !type.IsGenericType);
        }
        static IEnumerable<Type> GetFeatures()
        {
            return typeof(Endpoint).Assembly.GetTypes()
                .Where(type => typeof(Feature).IsAssignableFrom(type) && type.IsPublic && !type.IsAbstract);
        }

        static IEnumerable<Type> GetAttributeTypes()
        {
            return typeof(Endpoint).Assembly.GetTypes()
                .Where(type => type.Namespace != null &&
                               typeof(Attribute).IsAssignableFrom(type) &&
                               //Ignore log4net attributes
                               !type.Namespace.Contains("log4net") &&
                               //Ignore Newtonsoft attributes
                               !type.Namespace.Contains("Newtonsoft") &&
                               //Ignore JetBrains annotations
                               !type.Namespace.Contains("JetBrains") &&
                               //Ignore LightInject attributes
                               !type.Namespace.Contains("LightInject") &&
                               !type.Namespace.Contains("SimpleJson") &&
                               //TODO: remove when gitversion is updated
                               !type.Name.EndsWith("ReleaseDateAttribute") &&
                               !type.Name.EndsWith("NugetVersionAttribute"));
        }
    }
}