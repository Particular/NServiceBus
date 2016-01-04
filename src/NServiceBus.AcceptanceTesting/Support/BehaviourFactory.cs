namespace NServiceBus.AcceptanceTesting.Support
{
    public interface IEndpointConfigurationFactory
    {
        EndpointConfiguration Get();
        ScenarioContext ScenarioContext { get; set; }
    }
}