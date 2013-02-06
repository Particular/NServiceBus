namespace NServiceBus.AcceptanceTesting.Support
{
    public interface IEndpointBehaviorFactory
    {
        EndpointBehavior Get();
    }
}