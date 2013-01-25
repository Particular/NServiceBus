namespace NServiceBus.IntegrationTests.Support
{
    using System;

    public interface IScenarioWithEndpointBehavior
    {
        IScenarioWithEndpointBehavior WithEndpoint<T>() where T:EndpointBuilder;

        IScenarioWithEndpointBehavior WithEndpoint<T>(BehaviorContext context) where T : EndpointBuilder;

        IScenarioWithEndpointBehavior WithEndpoint<T>(Func<BehaviorContext> context) where T : EndpointBuilder;
        IScenarioWithEndpointBehavior Done<T>(Func<T, bool> func) where T : BehaviorContext;

        void Run();

        IAdvancedScenarioWithEndpointBehavior Repeat(Action<RunDescriptorsBuilder> runtimeDescriptor);

    }

    public interface IAdvancedScenarioWithEndpointBehavior
    {
        IAdvancedScenarioWithEndpointBehavior Should<T>(Action<T> should) where T : BehaviorContext;

        void Run();
    }
}