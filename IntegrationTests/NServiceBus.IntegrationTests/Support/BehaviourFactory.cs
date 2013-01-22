namespace NServiceBus.IntegrationTests.Support
{
    public interface IEndpointBehaviorFactory
    {
        EndpointBehavior Get();
    }
}