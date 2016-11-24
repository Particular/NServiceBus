namespace NServiceBus.AcceptanceTests.ScaleOut
{
    using AcceptanceTesting;

    public class DistributorContext : ScenarioContext
    {
        public bool ReceivedReadyMessage { get; set; }
        public string WorkerSessionId { get; set; }
    }
}