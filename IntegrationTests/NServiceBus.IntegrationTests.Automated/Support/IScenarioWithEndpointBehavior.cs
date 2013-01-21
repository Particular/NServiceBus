namespace NServiceBus.IntegrationTests.Automated.Support
{
    using System;

    public interface IScenarioWithEndpointBehavior
    {
        IScenarioWithEndpointBehavior WithEndpointBehaviour<T>() where T:BehaviorFactory;

        IScenarioWithEndpointBehavior WithEndpointBehaviour<T>(BehaviorContext context) where T : BehaviorFactory;

        void Run();
        void RunFor<T>() where T : ScenarioDescriptor;

        void Run(Action<RunDescriptorsBuilder> runtimeDescriptor);
    }
}