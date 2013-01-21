namespace NServiceBus.IntegrationTests.Support
{
    public interface BehaviorFactory
    {
        EndpointBehavior Get();
    }
}