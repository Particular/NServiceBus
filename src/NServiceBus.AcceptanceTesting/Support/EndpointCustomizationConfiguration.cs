namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class EndpointCustomizationConfiguration
    {
        public EndpointCustomizationConfiguration()
        {
            TypesToExclude = new List<Type>();
            TypesToInclude = new List<Type>();
            PublisherMetadata = new PublisherMetadata();
        }

        public IList<Type> TypesToExclude { get; }

        public IList<Type> TypesToInclude { get; }

        public Func<RunDescriptor, Task<EndpointConfiguration>> GetConfiguration { get; set; }

        public PublisherMetadata PublisherMetadata { get; }

        public string EndpointName
        {
            get => !string.IsNullOrEmpty(CustomEndpointName) ? CustomEndpointName : endpointName;
            set => endpointName = value;
        }

        public Type BuilderType { get; set; }

        public string CustomMachineName { get; set; }

        public string CustomEndpointName { get; set; }

        string endpointName;
    }
}