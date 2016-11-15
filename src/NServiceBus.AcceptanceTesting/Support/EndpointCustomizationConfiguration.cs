namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class EndpointCustomizationConfiguration
    {
        public EndpointCustomizationConfiguration()
        {
            UserDefinedConfigSections = new Dictionary<Type, object>();
            TypesToExclude = new List<Type>();
            TypesToInclude = new List<Type>();
            PublisherMetadata = new PublisherMetadata();
        }

        public IDictionary<Type, Type> EndpointMappings { get; set; }

        public IList<Type> TypesToExclude { get; set; }

        public IList<Type> TypesToInclude { get; set; }

        public Func<RunDescriptor, IDictionary<Type, string>, Task<EndpointConfiguration>> GetConfiguration { get; set; }

        public PublisherMetadata PublisherMetadata { get; private set; }

        public string EndpointName
        {
            get
            {
                if (!string.IsNullOrEmpty(CustomEndpointName))
                    return CustomEndpointName;
                return endpointName;
            }
            set { endpointName = value; }
        }

        public Type BuilderType { get; set; }

        public string AddressOfAuditQueue { get; set; }

        public IDictionary<Type, object> UserDefinedConfigSections { get; private set; }

        public string CustomMachineName { get; set; }

        public string CustomEndpointName { get; set; }

        public Type AuditEndpoint { get; set; }
        public bool SendOnly { get; set; }

        string endpointName;
    }
}