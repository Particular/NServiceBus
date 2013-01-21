namespace NServiceBus.IntegrationTests.Support
{
    using System;

    public interface IScenarioWithEndpointBehavior
    {
        IScenarioWithEndpointBehavior WithEndpointBehaviour<T>() where T:BehaviorFactory;

        IScenarioWithEndpointBehavior WithEndpointBehaviour<T>(BehaviorContext context) where T : BehaviorFactory;

        void Run();

        IAdvancedScenarioWithEndpointBehavior Repeat(Action<RunDescriptorsBuilder> runtimeDescriptor);
    }

    public interface IAdvancedScenarioWithEndpointBehavior
    {
        IAdvancedScenarioWithEndpointBehavior Should<T>(Action<T> should) where T : BehaviorContext;

        void Run();
    }
}