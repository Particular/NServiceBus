namespace NServiceBus.AcceptanceTests.Core.SelfVerification;

using System;
using System.Linq;
using System.Reflection;
using AcceptanceTesting;
using AcceptanceTesting.Support;
using NUnit.Framework;

[TestFixture]
public class EndpointNameEnforcementTests : NServiceBusAcceptanceTest
{
    const int endpointNameMaxLength = 77;

    [Test]
    public void EndpointName_should_not_exceed_maximum_length()
    {
        var testTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(IsEndpointClass);

        var violators = testTypes
            .Where(t => AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(t).Length > endpointNameMaxLength)
            .ToList();

        Assert.That(violators, Is.Empty, string.Join(",", violators));
    }

    [Test]
    public void Custom_endpoint_names_should_be_unique_across_fixtures()
    {
        var collisions = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => IsEndpointClass(t) && !t.IsAbstract)
            .Select(t => (Fixture: GetFixtureType(t), Name: ((IEndpointConfigurationFactory)Activator.CreateInstance(t)).Get().CustomEndpointName))
            .Where(e => !string.IsNullOrEmpty(e.Name))
            .GroupBy(e => e.Name)
            .Where(g => g.Select(e => e.Fixture).Distinct().Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(e => e.Fixture.Name).Distinct())}")
            .ToList();

        Assert.That(collisions, Is.Empty, string.Join(Environment.NewLine, collisions));
    }

    static Type GetFixtureType(Type t)
    {
        while (t.DeclaringType != null)
        {
            t = t.DeclaringType;
        }

        return t;
    }

    static bool IsEndpointClass(Type t) => endpointConfigurationBuilderType.IsAssignableFrom(t);

    static Type endpointConfigurationBuilderType = typeof(EndpointConfigurationBuilder);
}