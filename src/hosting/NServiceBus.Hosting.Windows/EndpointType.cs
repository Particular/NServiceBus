namespace NServiceBus.Hosting.Windows
{
    using System;
    using System.IO;
    using System.Reflection;
    using Arguments;
    using Utils;

    /// <summary>
    ///     Representation of an Endpoint Type with additional descriptive properties.
    /// </summary>
    public class EndpointType
    {
        internal EndpointType(HostArguments arguments, Type type) : this(type)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException("arguments");
            }
            this.arguments = arguments;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="EndpointType" /> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public EndpointType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            this.type = type;
            AssertIsValid();
            endpointConfiguration = Activator.CreateInstance(type);
        }

        internal Type Type
        {
            get { return type; }
        }

        public string EndpointConfigurationFile
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, type.Assembly.ManifestModule.Name + ".config"); }
        }

        public string EndpointVersion
        {
            get { return FileVersionRetriever.GetFileVersion(type); }
        }

        public string AssemblyQualifiedName
        {
            get { return type.AssemblyQualifiedName; }
        }

        public string EndpointName
        {
            get
            {
                var arr = type.GetCustomAttributes(typeof (EndpointNameAttribute), false);
                if (arr.Length == 1)
                {
                    return ((EndpointNameAttribute) arr[0]).Name;
                }

                var nameThisEndpoint = endpointConfiguration as INameThisEndpoint;
                if (nameThisEndpoint != null)
                {
                    return nameThisEndpoint.GetName();
                }

                if (arguments.EndpointName != null)
                {
                    return arguments.EndpointName;
                }
                return null;
            }
        }

        public string ServiceName
        {
            get
            {
                var serviceName = type.Namespace ?? type.Assembly.GetName().Name;

                if (arguments.ServiceName != null)
                {
                    serviceName = arguments.ServiceName;
                }

                return serviceName;
            }
        }

        void AssertIsValid()
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);

            if (constructor == null)
            {
                throw new InvalidOperationException(
                    "Endpoint configuration type needs to have a default constructor: " + type.FullName);
            }
        }

        readonly HostArguments arguments = new HostArguments(new string[0]);
        readonly object endpointConfiguration;
        readonly Type type;
    }
}