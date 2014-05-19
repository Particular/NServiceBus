namespace NServiceBus.AcceptanceTesting
{
    using System;
    using Support;

    public class Scenario
    {
        public static IScenarioWithEndpointBehavior<ScenarioContext> Define()
        {
            return Define<ScenarioContext>();
        }

        public static IScenarioWithEndpointBehavior<T> Define<T>() where T : ScenarioContext
        {
            return new ScenarioWithContext<T>(Activator.CreateInstance<T>);
        }

        public static IScenarioWithEndpointBehavior<T> Define<T>(T context) where T : ScenarioContext
        {
            return new ScenarioWithContext<T>(()=>context);
        }

        public static IScenarioWithEndpointBehavior<T> Define<T>(Func<T> contextFactory) where T : ScenarioContext
        {
            return new ScenarioWithContext<T>(contextFactory);
        }

    }
}