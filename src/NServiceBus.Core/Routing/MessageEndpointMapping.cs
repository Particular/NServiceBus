namespace NServiceBus.Config
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// A configuration element representing which message types map to which endpoint.
    /// </summary>
    public class MessageEndpointMapping : ConfigurationElement, IComparable<MessageEndpointMapping>
    {
        /// <summary>
        /// A string defining the message assembly, or single message type.
        /// </summary>
        [ConfigurationProperty("Messages", IsRequired = false)]
        public string Messages
        {
            get { return (string) this["Messages"]; }
            set { this["Messages"] = value; }
        }

        /// <summary>
        /// The endpoint named according to "queue@machine".
        /// </summary>
        [ConfigurationProperty("Endpoint", IsRequired = true)]
        public string Endpoint
        {
            get { return (string) this["Endpoint"]; }
            set { this["Endpoint"] = value; }
        }

        /// <summary>
        /// The message assembly for the endpoint mapping.
        /// </summary>
        [ConfigurationProperty("Assembly", IsRequired = false)]
        public string AssemblyName
        {
            get { return (string) this["Assembly"]; }
            set { this["Assembly"] = value; }
        }

        /// <summary>
        /// The fully qualified name of the message type. Define this if you want to map a single message type to the endpoint.
        /// </summary>
        /// <remarks>Type will take preference above namespace.</remarks>
        [ConfigurationProperty("Type", IsRequired = false)]
        public string TypeFullName
        {
            get { return (string) this["Type"]; }
            set { this["Type"] = value; }
        }

        /// <summary>
        /// The message namespace. Define this if you want to map all the types in the namespace to the endpoint.
        /// </summary>
        /// <remarks>Sub-namespaces will not be mapped.</remarks>
        [ConfigurationProperty("Namespace", IsRequired = false)]
        public string Namespace
        {
            get { return (string) this["Namespace"]; }
            set { this["Namespace"] = value; }
        }

        /// <summary>
        /// Comparison support.
        /// </summary>
        public int CompareTo(MessageEndpointMapping other)
        {
            if (!string.IsNullOrWhiteSpace(TypeFullName) || HaveMessagesMappingWithType(this))
            {
                if (!string.IsNullOrWhiteSpace(other.TypeFullName) || HaveMessagesMappingWithType(other))
                {
                    return 0;
                }

                return -1;
            }

            if (!string.IsNullOrWhiteSpace(Namespace))
            {
                if (!string.IsNullOrWhiteSpace(other.TypeFullName) || HaveMessagesMappingWithType(other))
                {
                    return 1;
                }

                if (!string.IsNullOrWhiteSpace(other.Namespace))
                {
                    return 0;
                }

                return -1;
            }

            if (!string.IsNullOrWhiteSpace(other.TypeFullName) || HaveMessagesMappingWithType(other))
            {
                return 1;
            }

            if (!string.IsNullOrWhiteSpace(other.Namespace))
            {
                return 1;
            }

            if (!string.IsNullOrWhiteSpace(other.AssemblyName) || !string.IsNullOrWhiteSpace(other.Messages))
            {
                return 0;
            }

            return -1;
        }

        /// <summary>
        /// Uses the configuration properties to configure the endpoint mapping.
        /// </summary>
        public void Configure(Action<Type, string> mapTypeToEndpoint)
        {
            Guard.AgainstNull(nameof(mapTypeToEndpoint), mapTypeToEndpoint);
            if (!string.IsNullOrWhiteSpace(Messages))
            {
                ConfigureEndpointMappingUsingMessagesProperty(mapTypeToEndpoint);
                return;
            }

            var address = Endpoint;
            var assemblyName = AssemblyName;
            var ns = Namespace;
            var typeFullName = TypeFullName;

            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                throw new ArgumentException("Could not process message endpoint mapping. The Assembly property is not defined. Either the Assembly or Messages property is required.");
            }

            var a = GetMessageAssembly(assemblyName);

            if (!string.IsNullOrWhiteSpace(typeFullName))
            {
                try
                {
                    var t = a.GetType(typeFullName, false);

                    if (t == null)
                    {
                        throw new ArgumentException($"Could not process message endpoint mapping. Cannot find the type '{typeFullName}' in the assembly '{assemblyName}'. Ensure that you are using the full name for the type.");
                    }

                    mapTypeToEndpoint(t, address);

                    return;
                }
                catch (BadImageFormatException ex)
                {
                    throw new ArgumentException($"Could not process message endpoint mapping. Could not load the assembly or one of its dependencies for type '{typeFullName}' in the assembly '{assemblyName}'", ex);
                }
                catch (FileLoadException ex)
                {
                    throw new ArgumentException($"Could not process message endpoint mapping. Could not load the assembly or one of its dependencies for type '{typeFullName}' in the assembly '{assemblyName}'", ex);
                }
            }

            var messageTypes = a.GetTypes().AsQueryable();

            if (!string.IsNullOrEmpty(ns))
            {
                messageTypes = messageTypes.Where(t => !string.IsNullOrWhiteSpace(t.Namespace) && t.Namespace.Equals(ns, StringComparison.InvariantCultureIgnoreCase));
            }

            foreach (var t in messageTypes)
            {
                mapTypeToEndpoint(t, address);
            }
        }

        void ConfigureEndpointMappingUsingMessagesProperty(Action<Type, string> mapTypeToEndpoint)
        {
            var address = Endpoint;
            var messages = Messages;

            try
            {
                var messageType = Type.GetType(messages, false);
                if (messageType != null)
                {
                    mapTypeToEndpoint(messageType, address);
                    return;
                }
            }
            catch (BadImageFormatException ex)
            {
                throw new ArgumentException(string.Format("Could not process message endpoint mapping. Could not load the assembly or one of its dependencies for type: " + messages), ex);
            }
            catch (FileLoadException ex)
            {
                throw new ArgumentException(string.Format("Could not process message endpoint mapping. Could not load the assembly or one of its dependencies for type: " + messages), ex);
            }

            var messagesAssembly = GetMessageAssembly(messages);

            foreach (var t in messagesAssembly.GetTypes())
            {
                mapTypeToEndpoint(t, address);
            }
        }

        static Assembly GetMessageAssembly(string assemblyName)
        {
            try
            {
                return Assembly.Load(assemblyName);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Could not process message endpoint mapping. Problem loading message assembly: " + assemblyName, ex);
            }
        }

        static bool HaveMessagesMappingWithType(MessageEndpointMapping mapping)
        {
            if (string.IsNullOrWhiteSpace(mapping.Messages))
            {
                return false;
            }

            return Type.GetType(mapping.Messages, false) != null;
        }
    }
}