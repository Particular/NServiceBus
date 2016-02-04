namespace NServiceBus.AcceptanceTesting.Support
{
    public interface IEndpointConfigurationFactory
    {
        EndpointCustomizationConfiguration Get();
        ScenarioContext ScenarioContext { get; set; }
    }
}