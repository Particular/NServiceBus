namespace NServiceBus.Hosting.Tests
{
    namespace EndpointTypeTests
    {
        using System;
        using Windows;
        using Windows.Arguments;
        using NUnit.Framework;

        public abstract class TestContext
        {
            protected EndpointType EndpointType;

// ReSharper disable once NotAccessedField.Global
            protected string TestValue;
        }

        [TestFixture]
        public class OtherProperty_Getter_Tests : TestContext
        {
            [TestFixtureSetUp]
            public void TestFixtureSetup()
            {
                EndpointType = new EndpointType(typeof (TestEndpointType));
            }

            [Test]
            public void the_assemblyQualifiedName_getter_should_not_blow_up()
            {
                TestValue = EndpointType.AssemblyQualifiedName;
            }

            [Test]
            public void the_endpointConfigurationFile_getter_should_not_blow_up()
            {
                TestValue = EndpointType.EndpointConfigurationFile;
            }

            [Test]
            public void the_endpointVersion_getter_should_not_blow_up()
            {
                TestValue = EndpointType.EndpointVersion;
            }
        }

        [TestFixture]
        public class EndpointName_Getter_Tests : TestContext
        {
            [SetUp]
            public void Setup()
            {
                // configuration hangs around between tests - have to clear it
                Configure.With().DefineEndpointName((Func<string>) null);
            }

            HostArguments hostArguments;

            [TestFixtureSetUp]
            public void TestFixtureSetup()
            {
                hostArguments = new HostArguments(new string[0])
                    {
                        EndpointName = "EndpointNameFromHostArgs"
                    };
            }

            [Test]
            public void when_endpointName_attribute_exists_it_should_have_first_priority()
            {
                Configure.With().DefineEndpointName("EndpointNameFromConfiguration");
                EndpointType = new EndpointType(hostArguments, typeof (TestEndpointTypeWithEndpointNameAttribute));

                Assert.AreEqual("EndpointNameFromAttribute", EndpointType.EndpointName);
            }

            [Test]
            [Ignore("this hasn't been implemented yet as far as i can tell")]
            public void when_endpointName_is_provided_via_configuration_it_should_have_second_priority()
            {
                Configure.With().DefineEndpointName("EndpointNameFromConfiguration");
                EndpointType = new EndpointType(hostArguments, typeof (TestEndpointType));

                Assert.AreEqual("EndpointNameFromConfiguration", EndpointType.EndpointName);
            }

            [Test]
            public void when_endpointName_is_provided_via_hostArgs_it_should_have_third_priority()
            {
                EndpointType = new EndpointType(hostArguments, typeof (TestEndpointType));

                Assert.AreEqual("EndpointNameFromHostArgs", EndpointType.EndpointName);
            }

            [Test]
            [Ignore(
                "not sure how to test this when interface marked as obsolete - get build errors if interface is referenced"
                )]
            public void when_iNameThisEndpoint_is_implemented_it_should_have_second_priority()
            {
            }

            [Test]
            public void when_no_EndpointName_defined_it_should_return_null()
            {
                hostArguments.EndpointName = null;
                EndpointType = new EndpointType(hostArguments, typeof (TestEndpointType));
                Assert.IsNull(EndpointType.EndpointName);
            }
        }

        [TestFixture]
        public class ServiceName_Getter_Tests : TestContext
        {
            HostArguments hostArguments;

            [Test]
            public void
                when_serviceName_is_not_provided_via_hostArgs_and_endpoint_has_a_namespace_it_should_use_the_namespace()
            {
                hostArguments = new HostArguments(new string[0]);
                EndpointType = new EndpointType(hostArguments, typeof (TestEndpointType));

                Assert.AreEqual("NServiceBus.Hosting.Tests.EndpointTypeTests", EndpointType.ServiceName);
            }

            [Test]
            public void
                when_serviceName_is_not_provided_via_hostArgs_and_endpoint_has_no_namespace_it_should_use_the_assembly_name
                ()
            {
                hostArguments = new HostArguments(new string[0]);
                EndpointType = new EndpointType(hostArguments, typeof (TestEndpointTypeWithoutANamespace));

                Assert.AreEqual("NServiceBus.Hosting.Tests", EndpointType.ServiceName);
            }

            [Test]
            public void when_serviceName_is_provided_via_hostArgs_it_should_have_first_priority()
            {
                hostArguments = new HostArguments(new string[0])
                    {
                        ServiceName = "ServiceNameFromHostArgs"
                    };
                EndpointType = new EndpointType(hostArguments, typeof (TestEndpointType));

                Assert.AreEqual("ServiceNameFromHostArgs", EndpointType.ServiceName);
            }
        }

        [TestFixture]
        public class Constructor_Tests
        {
            [Test]
            [ExpectedException(typeof (InvalidOperationException),
                ExpectedMessage = "Endpoint configuration type needs to have a default constructor",
                MatchType = MessageMatch.StartsWith)]
            public void When_type_does_not_have_empty_public_constructor_it_should_blow_up()
            {
                new EndpointType(typeof (TypeWithoutEmptyPublicConstructor));
            }
        }

        class TypeWithoutEmptyPublicConstructor
        {
            // ReSharper disable once UnusedParameter.Local
            public TypeWithoutEmptyPublicConstructor(object foo)
            {
            }
        }

        class TestEndpointType
        {
        }

        [EndpointName("EndpointNameFromAttribute")]
        class TestEndpointTypeWithEndpointNameAttribute
        {
        }
    }
}

class TestEndpointTypeWithoutANamespace
{
}