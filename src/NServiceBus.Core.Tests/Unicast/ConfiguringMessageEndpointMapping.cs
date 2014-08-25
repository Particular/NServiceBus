namespace NServiceBus.Unicast.Tests.Messages
{
    public class MessageE : IMessage
    {
    }

    public class MessageF : IMessage
    {
    }
}

namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Messages;
    using Messages.ANamespace;
    using Messages.ANamespace.ASubNamespace;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class Configuring_message_endpoint_mapping
    {
        public IDictionary<Type, string> Configure(Action<MessageEndpointMapping> setupMapping)
        {
            var mappings = new Dictionary<Type, string>();

            var mapping = new MessageEndpointMapping{ Endpoint = "SomeEndpoint" };

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