namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;

    public class EndpointConfiguration
    {
        public IDictionary<Type, Type> EndpointMappings { get; set; }

        public Func<RunDescriptor, IDictionary<Type, string>, Configure> GetConfiguration { get; set; }

        public string EndpointName { get; set; }

        public Type BuilderType { get; set; }

        public string AppConfigPath { get; set; }

        public Address AddressOfAuditQueue { get; set; }

        public IDictionary<Type,object> UserDefinedConfigSections { get; set; }
    }
}