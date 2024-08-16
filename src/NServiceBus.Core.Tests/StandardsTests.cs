namespace NServiceBus.Core.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NServiceBus.Features;
using NServiceBus.Logging;
using NUnit.Framework;

[TestFixture]
public class StandardsTests
{
    [Test]
    public void VerifyFeatureNaming()
    {
        foreach (var featureType in GetFeatures())
        {
            Assert.That(featureType.Namespace, Is.EqualTo("NServiceBus.Features"), "Features should be in the NServiceBus.Features namespace. " + featureType.FullName);
            Assert.That(featureType.Name, Does.Not.EndWith("Feature"), "Features should not be suffixed with 'Feature'. " + featureType.FullName);
            if (featureType.IsPublic)
            {
                var constructorInfo = featureType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Array.Empty<Type>(), null);
                Assert.That(constructorInfo.IsPublic, Is.False, "Features should have an internal constructor. " + featureType.FullName);
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
                !x.FullName.StartsWith("FastExpressionCompiler") &&
                // TODO: This can be removed once this bug is fixed in Roslyn: https://github.com/dotnet/roslyn/issues/72539
                // so that we can use inline arrays in public code without breaking this test
                !x.FullName.Contains("__InlineArray") &&
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
                    Assert.That(field.IsStatic, Is.True, "Logger fields should be static " + type.FullName);
                }
            }
        }
    }

    [Test]
    public void VerifyBehaviorNaming()
    {
        foreach (var featureType in GetBehaviors())
        {
            Assert.That(featureType.IsPublic, Is.False, "Behaviors should internal " + featureType.FullName);
            Assert.That(featureType.Namespace, Is.EqualTo("NServiceBus"), "Behaviors should be in the NServiceBus namespace since it reduces the 'wall of text' problem when looking at pipeline stack traces. " + featureType.FullName);
            Assert.That(featureType.Name.EndsWith("Terminator") || featureType.Name.EndsWith("Behavior") || featureType.Name.EndsWith("Connector"), Is.True, "Behaviors should be suffixed with 'Behavior' or 'Connector'. " + featureType.FullName);
        }
    }

    [Test]
    public void VerifyAttributesAreSealed()
    {
        foreach (var attributeType in GetAttributeTypes())
        {
            Assert.That(attributeType.IsSealed, Is.True, attributeType.FullName + " should be sealed.");
        }
    }

    static IEnumerable<Type> GetBehaviors()
    {
        return typeof(Endpoint).Assembly.GetTypes()
            .Where(type => type.GetInterfaces().Any(face => face.Name == nameof(NServiceBus.Pipeline.IBehavior)) && !type.IsAbstract && !type.IsGenericType);
    }
    static IEnumerable<Type> GetFeatures()
    {
        return typeof(Endpoint).Assembly.GetTypes()
            .Where(type => typeof(Feature).IsAssignableFrom(type) && type.IsPublic && !type.IsAbstract);
    }

    static IEnumerable<Type> GetAttributeTypes()
    {
        return typeof(Endpoint).Assembly.GetTypes()
            .Where(type => typeof(Attribute).IsAssignableFrom(type));
    }
}