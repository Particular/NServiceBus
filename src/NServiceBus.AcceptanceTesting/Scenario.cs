namespace NServiceBus.AcceptanceTesting;

using System;
using Support;

public static class Scenario
{
    public static IScenarioWithEndpointBehavior<T> Define<T>() where T : ScenarioContext, new()
        => new ScenarioWithContext<T>(static _ => { });

    public static IScenarioWithEndpointBehavior<T> Define<T>(Action<T> contextInitializer) where T : ScenarioContext, new()
        => new ScenarioWithContext<T>(contextInitializer);
}