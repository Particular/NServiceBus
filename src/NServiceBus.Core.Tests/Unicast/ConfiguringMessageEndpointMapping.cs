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
    using System.Configuration;
    using System.Linq;
    using Messages;
    using Messages.ANamespace;
    using Messages.ANamespace.ASubNamespace;
    using NServiceBus.Config;
    using NServiceBus.Config.ConfigurationSource;
    using NUnit.Framework;
    using Routing;

    public class Configuring_message_endpoint_mapping
    {
        public IDictionary<Type, Address> Configure(Action<MessageEndpointMapping> setupMapping)
        {
            var mappings = new Dictionary<Type, Address>();

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
    public class The_more_specific_mappings
    {
        [Test, Explicit("For some reason the build server is having issues with this.")]
        public void Should_take_precedence()
        {
            Configure.With(new Type[] {})
                .DefineEndpointName("Foo")
                .CustomConfigurationSource(new CustomUnicastBusConfig())
                .DefaultBuilder()
                .UnicastBus()
                .CreateBus();

            var messageOwners = Configure.Instance.Builder.Build<StaticMessageRouter>();

            Assert.AreEqual("Type", messageOwners.GetMultiDestinationFor(typeof(MessageA)).Single().Queue);
            Assert.AreEqual("Namespace", messageOwners.GetMultiDestinationFor(typeof(MessageB)).Single().Queue);
            Assert.AreEqual("Assembly", messageOwners.GetMultiDestinationFor(typeof(MessageD)).Single().Queue);
            Assert.AreEqual("MessagesWithType", messageOwners.GetMultiDestinationFor(typeof(MessageE)).Single().Queue);
            Assert.AreEqual("Namespace", messageOwners.GetMultiDestinationFor(typeof(MessageF)).Single().Queue);
        }

        class CustomUnicastBusConfig : IConfigurationSource
        {
            public T GetConfiguration<T>() where T : class, new()
            {
                var mappingByType = new MessageEndpointMapping { Endpoint = "Type", TypeFullName = "NServiceBus.Unicast.Tests.Messages.MessageA", AssemblyName = "NServiceBus.Core.Tests" };
                var mappingByNamespace = new MessageEndpointMapping { Endpoint = "Namespace", Namespace = "NServiceBus.Unicast.Tests.Messages", AssemblyName = "NServiceBus.Core.Tests" };
                var mappingByAssembly = new MessageEndpointMapping { Endpoint = "Assembly", AssemblyName = "NServiceBus.Core.Tests" };
                var mappingByMessagesWithType = new MessageEndpointMapping { Endpoint = "MessagesWithType", Messages = "NServiceBus.Unicast.Tests.Messages.MessageE, NServiceBus.Core.Tests" };
                var mappings = new MessageEndpointMappingCollection { mappingByNamespace, mappingByType, mappingByAssembly, mappingByMessagesWithType };


                var type = typeof(T);

                if (type == typeof(MessageForwardingInCaseOfFaultConfig))
                    return new MessageForwardingInCaseOfFaultConfig
                    {
                        ErrorQueue = "error"
                    } as T;

                if (type == typeof(UnicastBusConfig))
                {

                    return new UnicastBusConfig
                    {
                        MessageEndpointMappings = mappings,
                    } as T;

                }

                if (type == typeof(Logging))
                    return new Logging
                    {
                        Threshold = "WARN"
                    } as T;


                return ConfigurationManager.GetSection(type.Name) as T;
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