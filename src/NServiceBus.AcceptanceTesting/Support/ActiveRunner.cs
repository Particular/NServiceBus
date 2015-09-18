namespace NServiceBus.AcceptanceTesting.Support
{
    using System.Threading.Tasks;

    class ActiveRunner
    {
        public EndpointRunner Instance { get; set; }
        public string EndpointName { get; set; }
        public Task<EndpointRunner.Result> InitializeTask { get; set; }
    }
}