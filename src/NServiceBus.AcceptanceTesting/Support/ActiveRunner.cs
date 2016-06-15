namespace NServiceBus.AcceptanceTesting.Support
{
    class ActiveRunner
    {
        public EndpointRunner Instance { get; set; }
        public string EndpointName { get; set; }
    }
}