namespace EndpointTypeDeterminerTests
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Reflection;
    using NServiceBus.Hosting.Helpers;
    using NServiceBus.Hosting.Tests;
    using NServiceBus.Hosting.Tests.EndpointTypeTests;
    using NServiceBus.Hosting.Windows;
    using NServiceBus.Hosting.Windows.Arguments;
    using NUnit.Framework;

    abstract class TestContext
    {
        protected AssemblyScanner AssemblyScanner;
        protected Type EndpointTypeDefinedInConfigurationFile = typeof(ConfigWithCustomTransport);
        protected Type RetrievedEndpointType;
        protected EndpointTypeDeterminer EndpointTypeDeterminer;
    }

    [TestFixture]
    class GetEndpointConfigurationTypeForHostedEndpoint_Tests : TestContext
    {
        HostArguments hostArguments;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            AssemblyScanner = new AssemblyScanner();
        }

        [Test]
        public void
            when_endpoint_type_is_not_provided_via_hostArgs_it_should_fall_through_to_other_modes_of_determining_endpoint_type
            ()
        {
            EndpointTypeDeterminer = new EndpointTypeDeterminer(AssemblyScanner,
                () => ConfigurationManager.AppSettings["EndpointConfigurationType"]);
            hostArguments = new HostArguments(new string[0]);

            // will match with config-based type
            RetrievedEndpointType = EndpointTypeDeterminer.GetEndpointConfigurationTypeForHostedEndpoint(hostArguments).Type;

            Assert.AreEqual(EndpointTypeDefinedInConfigurationFile, RetrievedEndpointType);
        }

        [Test]
        public void when_endpoint_type_is_provided_via_hostArgs_it_should_have_first_priority()
        {
            EndpointTypeDeterminer = new EndpointTypeDeterminer(AssemblyScanner,
                () => ConfigurationManager.AppSettings["EndpointConfigurationType"]);
            hostArguments = new HostArguments(new string[0])
            {
                EndpointConfigurationType = typeof(TestEndpointType).AssemblyQualifiedName
            };

            RetrievedEndpointType = EndpointTypeDeterminer.GetEndpointConfigurationTypeForHostedEndpoint(hostArguments).Type;

            Assert.AreEqual(typeof(TestEndpointType), RetrievedEndpointType);
        }

        [Test]
        [ExpectedException(typeof(ConfigurationErrorsException))]
        public void when_invalid_endpoint_type_is_provided_via_hostArgs_it_should_blow_up()
        {
            EndpointTypeDeterminer = new EndpointTypeDeterminer(AssemblyScanner,
                () => ConfigurationManager.AppSettings["EndpointConfigurationType"]);
            hostArguments = new HostArguments(new string[0])
            {
                EndpointConfigurationType = "I am an invalid type name"
            };

            RetrievedEndpointType = EndpointTypeDeterminer.GetEndpointConfigurationTypeForHostedEndpoint(hostArguments).Type;
        }
    }

    [TestFixture]
    class GetEndpointConfigurationType_Tests_2 : TestContext
    {
        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            AssemblyScanner = new AssemblyScanner();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException),
            ExpectedMessage = "Host doesn't support hosting of multiple endpoints",
            MatchType = MessageMatch.StartsWith)]
        public void when_multiple_endpoint_types_found_via_assembly_scanning_it_should_blow_up()
        {
            EndpointTypeDeterminer = new EndpointTypeDeterminer(AssemblyScanner, () => null);

            RetrievedEndpointType = EndpointTypeDeterminer.GetEndpointConfigurationType().Type;
        }
    }

    [TestFixture]
    class GetEndpointConfigurationType_Tests : TestContext
    {
        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            AssemblyScanner = new AssemblyScanner();
        }

        [Test]
        public void when_endpoint_type_is_provided_via_configuration_it_should_have_first_priority()
        {
            EndpointTypeDeterminer = new EndpointTypeDeterminer(AssemblyScanner,
                () => ConfigurationManager.AppSettings["EndpointConfigurationType"]);

            RetrievedEndpointType = EndpointTypeDeterminer.GetEndpointConfigurationType().Type;

            Assert.AreEqual(EndpointTypeDefinedInConfigurationFile, RetrievedEndpointType);
        }

        [Test]
        [ExpectedException(typeof(ConfigurationErrorsException),
            ExpectedMessage = "The 'EndpointConfigurationType' entry in the NServiceBus.Host.exe.config",
            MatchType = MessageMatch.StartsWith)]
        public void when_invalid_endpoint_type_is_provided_via_configuration_it_should_blow_up()
        {
            EndpointTypeDeterminer = new EndpointTypeDeterminer(AssemblyScanner,
                () => "I am an invalid type name");

            RetrievedEndpointType = EndpointTypeDeterminer.GetEndpointConfigurationType().Type;
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException),
            ExpectedMessage = "No endpoint configuration found in scanned assemblies",
            MatchType = MessageMatch.StartsWith)]
        public void when_no_endpoint_type_found_via_configuration_or_assembly_scanning_it_should_blow_up()
        {
            AssemblyScanner = new AssemblyScanner
            {
                IncludeExesInScan = false,
                AssembliesToSkip = new List<string>
                {
                    Assembly.GetExecutingAssembly().GetName().Name
                }
            };

            EndpointTypeDeterminer = new EndpointTypeDeterminer(AssemblyScanner, () => null);

            RetrievedEndpointType = EndpointTypeDeterminer.GetEndpointConfigurationType().Type;
        }
    }
}