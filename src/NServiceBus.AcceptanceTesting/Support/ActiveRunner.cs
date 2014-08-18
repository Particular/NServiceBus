namespace NServiceBus.AcceptanceTesting.Support
{
    using System;

    class ActiveRunner
    {
        public EndpointRunner Instance { get; set; }
        public string EndpointName { get; set; }
        public AppDomain AppDomain { get; set; }
    }
}