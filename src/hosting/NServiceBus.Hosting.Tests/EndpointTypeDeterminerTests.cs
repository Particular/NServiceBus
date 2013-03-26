using System;
using NServiceBus.Hosting.Windows;
using NServiceBus.Hosting.Windows.Arguments;
using NUnit.Framework;
using System.Configuration;
using System.Reflection;
using System.Reflection.Emit;
using NServiceBus.Hosting.Tests.EndpointTypeTests;
using NServiceBus.Hosting.Helpers;

namespace NServiceBus.Hosting.Tests
{
    namespace EndpointTypeDeterminerTests
    {
        public abstract class TestContext
        {
            protected EndpointTypeDeterminer Sut;
            protected AssemblyScannerResults AssemblyScannerResults;
            protected Type RetrievedEndpointType;
            protected Type EndpointTypeDefinedInConfigurationFile = typeof(ConfigWithCustomTransport);
        }

        [TestFixture]
        public class GetEndpointConfigurationTypeForHostedEndpoint_Tests : TestContext
        {
            HostArguments hostArguments;

            [TestFixtureSetUp]
            public void TestFixtureSetup() {
                AssemblyScannerResults = AssemblyScanner.GetScannableAssemblies();
            }

            [Test]
            public void when_endpoint_type_is_provided_via_hostargs_it_should_have_first_priority() {
                Sut = new EndpointTypeDeterminer(AssemblyScannerResults, () => ConfigurationManager.AppSettings["EndpointConfigurationType"]);
                hostArguments = new HostArguments(new string[0])
                    {
                        EndpointConfigurationType = typeof(TestEndpointType).AssemblyQualifiedName
                    };

                RetrievedEndpointType = Sut.GetEndpointConfigurationTypeForHostedEndpoint(hostArguments).Type;

                Assert.AreEqual(typeof(TestEndpointType), RetrievedEndpointType);
            }

            [Test]
            [ExpectedException(typeof(ConfigurationErrorsException))]
            public void when_invalid_endpoint_type_is_provided_via_hostargs_it_should_blow_up() {
                Sut = new EndpointTypeDeterminer(AssemblyScannerResults, () => ConfigurationManager.AppSettings["EndpointConfigurationType"]);
                hostArguments = new HostArguments(new string[0])
                    {
                        EndpointConfigurationType = "I am an invalid type name"
                    };

                RetrievedEndpointType = Sut.GetEndpointConfigurationTypeForHostedEndpoint(hostArguments).Type;
            }

            [Test]
            public void when_endpoint_type_is_not_provided_via_hostargs_it_should_fall_through_to_other_modes_of_determining_endpoint_type() {
                Sut = new EndpointTypeDeterminer(AssemblyScannerResults, () => ConfigurationManager.AppSettings["EndpointConfigurationType"]);
                hostArguments = new HostArguments(new string[0]);

                // will match with config-based type
                RetrievedEndpointType = Sut.GetEndpointConfigurationTypeForHostedEndpoint(hostArguments).Type;

                Assert.AreEqual(EndpointTypeDefinedInConfigurationFile, RetrievedEndpointType);
            }
        }

        [TestFixture]
        public class GetEndpointConfigurationType_Tests : TestContext
        {
            [Test]
            public void when_endpoint_type_is_provided_via_configuration_it_should_have_first_priority() {
                Sut = new EndpointTypeDeterminer(AssemblyScanner.GetScannableAssemblies(), () => ConfigurationManager.AppSettings["EndpointConfigurationType"]);

                RetrievedEndpointType = Sut.GetEndpointConfigurationType().Type;

                Assert.AreEqual(EndpointTypeDefinedInConfigurationFile, RetrievedEndpointType);
            }

            [Test]
            [ExpectedException(typeof(ConfigurationErrorsException), ExpectedMessage = "The 'EndpointConfigurationType' entry in the NServiceBus.Host.exe.config", MatchType = MessageMatch.StartsWith)]
            public void when_invalid_endpoint_type_is_provided_via_configuration_it_should_blow_up() {
                Sut = new EndpointTypeDeterminer(AssemblyScanner.GetScannableAssemblies(), () => "I am an invalid type name");

                RetrievedEndpointType = Sut.GetEndpointConfigurationType().Type;
            }

            [Test]
            public void when_endpoint_type_is_found_via_assembly_scanning_it_should_have_second_priority() {
                AssemblyScannerResults = new AssemblyScannerResults();
                Type endpointTypeInAssembly;
                var dynamicAssembly = BuildTestEndpointAssembly(out endpointTypeInAssembly);
                AssemblyScannerResults.Assemblies.Add(dynamicAssembly);
                Sut = new EndpointTypeDeterminer(AssemblyScannerResults, () => null);

                RetrievedEndpointType = Sut.GetEndpointConfigurationType().Type;

                Assert.AreEqual(endpointTypeInAssembly, RetrievedEndpointType);
            }

            [Test]
            [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "No endpoint configuration found in scanned assemblies", MatchType = MessageMatch.StartsWith)]
            public void when_no_endpoint_type_found_via_configuration_or_assembly_scanning_it_should_blow_up() {
                Sut = new EndpointTypeDeterminer(new AssemblyScannerResults(), () => null);

                RetrievedEndpointType = Sut.GetEndpointConfigurationType().Type;
            }

            [Test]
            [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Host doesn't support hosting of multiple endpoints", MatchType = MessageMatch.StartsWith)]
            public void when_multiple_endpoint_types_found_via_assembly_scanning_it_should_blow_up() {
                Sut = new EndpointTypeDeterminer(AssemblyScanner.GetScannableAssemblies(), () => null);

                RetrievedEndpointType = Sut.GetEndpointConfigurationType().Type;
            }

            Assembly BuildTestEndpointAssembly(out Type endpointType) {
                AppDomain appDomain = AppDomain.CurrentDomain;
                AssemblyName aname = new AssemblyName("MyDynamicAssembly");
                AssemblyBuilder assemBuilder = appDomain.DefineDynamicAssembly(aname, AssemblyBuilderAccess.Run);
                ModuleBuilder modBuilder = assemBuilder.DefineDynamicModule("DynModule");
                TypeBuilder tb = modBuilder.DefineType("TestEndpoint", TypeAttributes.Public, typeof(Object), new[] {typeof(IConfigureThisEndpoint)});
                endpointType = tb.CreateType();
                return assemBuilder;
            }
        }
    }
}