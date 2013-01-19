namespace NServiceBus.IntegrationTests.Automated.Support
{
    public interface BehaviorFactory
    {
        EndpointBehavior Get();
    }
}