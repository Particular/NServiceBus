namespace NServiceBus.IntegrationTests.Automated.Support
{
    public interface IScenarioWithEndpointBehavior
    {
        IScenarioWithEndpointBehavior WithEndpointBehaviour<T>() where T:BehaviorFactory;

        IScenarioWithEndpointBehavior WithEndpointBehaviour<T>(BehaviorContext context) where T : BehaviorFactory;

        void Run();
    }
}