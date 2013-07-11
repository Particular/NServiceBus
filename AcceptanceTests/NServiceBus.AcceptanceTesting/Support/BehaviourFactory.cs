namespace NServiceBus.AcceptanceTesting.Support
{
    public interface IEndpointConfigurationFactory
    {
        EndpointConfiguration Get();
    }
}