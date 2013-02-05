namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class EndpointBehavior
    {
        public List<Action<IBus>> Givens { get; set; }

        public IDictionary<Type,Type> EndpointMappings { get; set; }

        public List<Action<IBus,ScenarioContext>> Whens { get; set; }

        public Func<RunDescriptor, IDictionary<Type, string>, Configure> GetConfiguration { get; set; }

        public string EndpointName{ get; set; }

        public Type BuilderType { get; set; }

        public string AppConfigPath { get; set; }

        public Address AddressOfAuditQueue { get; set; }
    }
}