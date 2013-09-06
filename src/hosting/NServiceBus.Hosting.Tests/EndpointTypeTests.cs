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
            protected EndpointType Sut;
            protected string TestValue;
        }

        [TestFixture]
        public class OtherProperty_Getter_Tests : TestContext
        {
            [TestFixtureSetUp]
            public void TestFixtureSetup()
            {
                Sut = new EndpointType(typeof (TestEndpointType));
            }

            [Test]
            public void the_assemblyqualifiedname_getter_should_not_blow_up()
            {
                TestValue = Sut.AssemblyQualifiedName;
            }

            [Test]
            public void the_endpointconfigurationfile_getter_should_not_blow_up()
            {
                TestValue = Sut.EndpointConfigurationFile;
            }

            [Test]
            public void the_endpointversion_getter_should_not_blow_up()
            {
                TestValue = Sut.EndpointVersion;
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
            public void when_endpointname_attribute_exists_it_should_have_first_priority()
            {
                Configure.With().DefineEndpointName("EndpointNameFromConfiguration");
                Sut = new EndpointType(hostArguments, typeof (TestEndpointTypeWithEndpointNameAttribute));

                Assert.AreEqual("EndpointNameFromAttribute", Sut.EndpointName);
            }

            [Test]
            [Ignore("this hasn't been implemented yet as far as i can tell")]
            public void when_endpointname_is_provided_via_configuration_it_should_have_second_priority()
            {
                Configure.With().DefineEndpointName("EndpointNameFromConfiguration");
                Sut = new EndpointType(hostArguments, typeof (TestEndpointType));

                Assert.AreEqual("EndpointNameFromConfiguration", Sut.EndpointName);
            }

            [Test]
            public void when_endpointname_is_provided_via_hostargs_it_should_have_third_priority()
            {
                Sut = new EndpointType(hostArguments, typeof (TestEndpointType));

                Assert.AreEqual("EndpointNameFromHostArgs", Sut.EndpointName);
            }

            [Test]
            [Ignore(
                "not sure how to test this when interface marked as obsolete - get build errors if interface is referenced"
                )]
            public void when_inamethisendpoint_is_implemented_it_should_have_second_priority()
            {
            }

            [Test]
            public void when_no_endpointname_defined_it_should_return_null()
            {
                hostArguments.EndpointName = null;
                Sut = new EndpointType(hostArguments, typeof (TestEndpointType));
                Assert.IsNull(Sut.EndpointName);
            }
        }

        [TestFixture]
        public class ServiceName_Getter_Tests : TestContext
        {
            HostArguments hostArguments;

            [Test]
            public void
                when_servicename_is_not_provided_via_hostargs_and_endpoint_has_a_namespace_it_should_use_the_namespace()
            {
                hostArguments = new HostArguments(new string[0]);
                Sut = new EndpointType(hostArguments, typeof (TestEndpointType));

                Assert.AreEqual("NServiceBus.Hosting.Tests.EndpointTypeTests", Sut.ServiceName);
            }

            [Test]
            public void
                when_servicename_is_not_provided_via_hostargs_and_endpoint_has_no_namespace_it_should_use_the_assembly_name
                ()
            {
                hostArguments = new HostArguments(new string[0]);
                Sut = new EndpointType(hostArguments, typeof (TestEndpointTypeWithoutANamespace));

                Assert.AreEqual("NServiceBus.Hosting.Tests", Sut.ServiceName);
            }

            [Test]
            public void when_servicename_is_provided_via_hostargs_it_should_have_first_priority()
            {
                hostArguments = new HostArguments(new string[0])
                    {
                        ServiceName = "ServiceNameFromHostArgs"
                    };
                Sut = new EndpointType(hostArguments, typeof (TestEndpointType));

                Assert.AreEqual("ServiceNameFromHostArgs", Sut.ServiceName);
            }
        }

        [TestFixture]
        public class Constructor_Tests
        {
            protected EndpointType Sut;

            [Test]
            [ExpectedException(typeof (InvalidOperationException),
                ExpectedMessage = "Endpoint configuration type needs to have a default constructor",
                MatchType = MessageMatch.StartsWith)]
            public void When_type_does_not_have_empty_public_constructor_it_should_blow_up()
            {
                Sut = new EndpointType(typeof (TypeWithoutEmptyPublicConstructor));
            }
        }

        internal class TypeWithoutEmptyPublicConstructor
        {
            public TypeWithoutEmptyPublicConstructor(object foo)
            {
            }
        }

        internal class TestEndpointType
        {
        }

        [EndpointName("EndpointNameFromAttribute")]
        internal class TestEndpointTypeWithEndpointNameAttribute
        {
        }
    }
}

internal class TestEndpointTypeWithoutANamespace
{
}