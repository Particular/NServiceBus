namespace NServiceBus.IntegrationTests.Automated.Support
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Config;

    public class EndpointScenario
    {
        public string EndpointName { get; set; }

        public List<Action<IBus>> Givens { get; set; }

        public MessageEndpointMappingCollection EndpointMappings { get; set; }

        public Func<bool> Done { get; set; }

        public List<Action<IBus>> Whens { get; set; }

        public List<Action<Configure>> SetupActions { get; set; }

        public List<Action> Assertions { get; set; }
    }
}