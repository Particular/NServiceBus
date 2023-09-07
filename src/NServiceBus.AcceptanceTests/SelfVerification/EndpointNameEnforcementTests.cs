namespace NServiceBus.AcceptanceTests.Core.SelfVerification
{
    using System;
    using System.Linq;
    using System.Reflection;
    using AcceptanceTesting;
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

            CollectionAssert.IsEmpty(violators, string.Join(",", violators));
        }

        static bool IsEndpointClass(Type t) => endpointConfigurationBuilderType.IsAssignableFrom(t);

        static Type endpointConfigurationBuilderType = typeof(EndpointConfigurationBuilder);
    }
}