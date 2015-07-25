namespace NServiceBus.AcceptanceTesting
{
    using System;
    using Support;

    public class Scenario
    {
        public static IScenarioWithEndpointBehavior<T> Define<T>() where T : ScenarioContext, new()
        {
            Func<T> contextFactory = () => new T();
            return new ScenarioWithContext<T>(contextFactory);
        }

        public static IScenarioWithEndpointBehavior<T> Define<T>(T context) where T : ScenarioContext, new()
        {
            return new ScenarioWithContext<T>(()=>context);
        }

        public static IScenarioWithEndpointBehavior<T> Define<T>(Func<T> contextFactory) where T : ScenarioContext, new()
        {
            return new ScenarioWithContext<T>(contextFactory);
        }

    }
}