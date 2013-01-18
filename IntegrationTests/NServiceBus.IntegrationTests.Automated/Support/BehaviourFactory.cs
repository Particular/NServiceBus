namespace NServiceBus.IntegrationTests.Automated.Support
{
    public interface BehaviourFactory
    {
        EndpointBehaviour Get();
    }
}