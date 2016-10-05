namespace NServiceBus.AcceptanceTesting
{
    using System;
    using Support;

    public class Scenario
    {
        public static IScenarioWithEndpointBehavior<T> Define<T>() where T : ScenarioContext, new()
        {
            return new ScenarioWithContext<T>(c => { });
        }

        public static IScenarioWithEndpointBehavior<T> Define<T>(Action<T> contextInitializer) where T : ScenarioContext, new()
        {
            return new ScenarioWithContext<T>(contextInitializer);
        }
    }
}