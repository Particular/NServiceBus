namespace NServiceBus.IntegrationTests.Support
{
    using System;

    public interface IScenarioWithEndpointBehavior
    {
        IScenarioWithEndpointBehavior WithEndpoint<T>() where T:EndpointBuilder;

        IScenarioWithEndpointBehavior WithEndpoint<T>(BehaviorContext context) where T : EndpointBuilder;

        IScenarioWithEndpointBehavior WithEndpoint<T>(Func<BehaviorContext> context) where T : EndpointBuilder;

        void Run();

        IAdvancedScenarioWithEndpointBehavior Repeat(Action<RunDescriptorsBuilder> runtimeDescriptor);
        IScenarioWithEndpointBehavior WithEndpointNamingConvention(Func<Type,string> customConvention);
    }

    public interface IAdvancedScenarioWithEndpointBehavior
    {
        IAdvancedScenarioWithEndpointBehavior Should<T>(Action<T> should) where T : BehaviorContext;

        void Run();
    }
}