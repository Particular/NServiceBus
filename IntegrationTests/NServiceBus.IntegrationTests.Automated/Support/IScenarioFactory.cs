namespace NServiceBus.IntegrationTests.Automated.Support
{
    public interface IScenarioFactory
    {
        EndpointScenario Get();
    }
}