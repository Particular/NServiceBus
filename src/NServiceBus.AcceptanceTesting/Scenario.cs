namespace NServiceBus.AcceptanceTesting
{
    using System;
    using Logging;
    using Support;

    public class Scenario
    {
        public static Func<ScenarioContext, ILoggerFactory> GetLoggerFactory = _ => new ContextAppenderFactory();

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