using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.Config;
using NServiceBus.Unicast.Tests.Messages;
using NServiceBus.Unicast.Tests.Messages.ANamespace;
using NServiceBus.Unicast.Tests.Messages.ANamespace.ASubNamespace;
using NUnit.Framework;

namespace NServiceBus.Unicast.Tests.Messages
{
    public class MessageE
    {
    }

    public class MessageF
    {
    }
}

namespace NServiceBus.Unicast.Tests
{
    using NServiceBus.Config.ConfigurationSource;

    public class Configuring_message_endpoint_mapping
    {
        public IDictionary<Type, Address> Configure(Action<MessageEndpointMapping> setupMapping)
        {
            var mappings = new Dictionary<Type, Address>();

            var mapping = new MessageEndpointMapping() { Endpoint = "SomeEndpoint" };

            setupMapping(mapping);

            mapping.Configure((t, a) => mappings[t] = a);

            return mappings;
        }

        protected void ConfigureShouldMapTypes(Action<MessageEndpointMapping> messageAction, params Type[] expected)
        {
            var mappings = Configure(messageAction).Keys.ToArray();

            Assert.That(expected, Is.SubsetOf(mappings));
        }
    }

    [TestFixture]
    public class The_more_specific_mappings
    {
        [Test,Explicit("Fails randomly")]
        public void Should_take_precedence()
        {
            Configure.With()
                .DefineEndpointName("Foo")
                .DefaultBuilder()
                .UnicastBus();

            var messageOwners = Configure.Instance.Builder.Build<UnicastBus>().MessageOwners;

            Assert.AreEqual("type", messageOwners[typeof(MessageA)].Queue);
            Assert.AreEqual("namespace", messageOwners[typeof(MessageB)].Queue);
            Assert.AreEqual("assembly", messageOwners[typeof(MessageD)].Queue);
            Assert.AreEqual("messageswithtype", messageOwners[typeof(MessageE)].Queue);
            Assert.AreEqual("namespace", messageOwners[typeof(MessageF)].Queue);
        }

        public class CustomUnicastBusConfig : IProvideConfiguration<UnicastBusConfig>
        {
            public UnicastBusConfig GetConfiguration()
            {
                var mappingByType = new MessageEndpointMapping { Endpoint = "Type", TypeFullName = "NServiceBus.Unicast.Tests.Messages.MessageA", AssemblyName = "NServiceBus.Core.Tests" };
                var mappingByNamespace = new MessageEndpointMapping { Endpoint = "Namespace", Namespace = "NServiceBus.Unicast.Tests.Messages", AssemblyName = "NServiceBus.Core.Tests" };
                var mappingByAssembly = new MessageEndpointMapping { Endpoint = "Assembly", AssemblyName = "NServiceBus.Core.Tests" };
                var mappingByMessagesWithType = new MessageEndpointMapping { Endpoint = "MessagesWithType", Messages = "NServiceBus.Unicast.Tests.Messages.MessageE, NServiceBus.Core.Tests" };
                var mappings = new MessageEndpointMappingCollection { mappingByNamespace, mappingByType, mappingByAssembly, mappingByMessagesWithType };

                return new UnicastBusConfig
                {
                    MessageEndpointMappings = mappings
                };
            }
        }
    }

    [TestFixture]
    public class When_configuring_an_endpoint_mapping_using_an_assembly_name_in_the_messages_property : Configuring_message_endpoint_mapping
    {
        [Test]
        public void Should_map_all_the_types_in_the_assembly()
        {
            ConfigureShouldMapTypes(m => m.Messages = "NServiceBus.Core.Tests",
                typeof(MessageA), typeof(MessageB), typeof(MessageC), typeof(MessageD));
        }
    }

    [TestFixture]
    public class When_configuring_an_endpoint_mapping_using_an_assembly_name_in_the_messages_property_that_does_not_exist : Configuring_message_endpoint_mapping
    {
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Should_fail()
        {
            Configure(m => m.Messages = "NServiceBus.Unicast.Tests.MessagesThatDoesNotExist");
        }
    }

    [TestFixture]
    public class When_configuring_an_endpoint_mapping_using_a_type_name_in_the_messages_property : Configuring_message_endpoint_mapping
    {
        [Test]
        public void Should_only_map_the_type()
        {
            ConfigureShouldMapTypes(m => m.Messages = "NServiceBus.Unicast.Tests.Messages.MessageA, NServiceBus.Core.Tests", typeof(MessageA));
        }
    }

    [TestFixture]
    public class When_configuring_an_endpoint_mapping_using_a_type_name_in_the_messages_property_that_does_not_exist : Configuring_message_endpoint_mapping
    {
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Should_fail()
        {
            Configure(m => m.Messages = "NServiceBus.Unicast.Tests.Messages.MessageThatDoesNotExist, NServiceBus.Core.Tests");
        }
    }

    [TestFixture]
    public class When_configuring_an_endpoint_mapping_using_only_the_assembly_property : Configuring_message_endpoint_mapping
    {
        [Test]
        public void Should_map_all_the_types_in_the_assembly()
        {
            ConfigureShouldMapTypes(m => m.AssemblyName = "NServiceBus.Core.Tests",
                typeof(MessageA), typeof(MessageB), typeof(MessageC), typeof(MessageD));
        }
    }

    [TestFixture]
    public class When_configuring_an_endpoint_mapping_using_the_assembly_property_with_an_assembly_that_does_not_exist : Configuring_message_endpoint_mapping
    {
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Should_fail()
        {
            Configure(m => m.AssemblyName = "NServiceBus.Unicast.Tests.MessagesThatDoesNotExist");
        }
    }

    [TestFixture]
    public class When_configuring_an_endpoint_mapping_using_the_type_property : Configuring_message_endpoint_mapping
    {
        [Test]
        public void Should_only_map_the_type()
        {
            ConfigureShouldMapTypes(m => { m.AssemblyName = "NServiceBus.Core.Tests"; m.TypeFullName = "NServiceBus.Unicast.Tests.Messages.MessageA"; }, typeof(MessageA));
        }
    }

    [TestFixture]
    public class When_configuring_an_endpoint_mapping_using_the_type_property_with_a_type_that_does_not_exist : Configuring_message_endpoint_mapping
    {
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Should_fail()
        {
            Configure(m => { m.AssemblyName = "NServiceBus.Core.Tests"; m.TypeFullName = "NServiceBus.Unicast.Tests.Messages.MessageThatDoesNotExist"; });
        }
    }

    [TestFixture]
    public class When_configuring_an_endpoint_mapping_using_the_namespace_property : Configuring_message_endpoint_mapping
    {
        [Test]
        public void Should_only_map_the_types_directly_in_the_namespace()
        {
            ConfigureShouldMapTypes(m => { m.AssemblyName = "NServiceBus.Core.Tests"; m.Namespace = "NServiceBus.Unicast.Tests.Messages.ANamespace"; }, typeof(MessageC));
        }
    }
}