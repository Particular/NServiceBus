namespace NServiceBus.AcceptanceTests;

using System;
using AcceptanceTesting;
using AcceptanceTesting.Support;

static class BehaviorBuilderRegistrationExtensions
{
    public static EndpointBehaviorBuilder<TContext> CustomRegistrations<TContext>(this EndpointBehaviorBuilder<TContext> builder, RegistrationApproach approach, Action<EndpointConfiguration> manual, Action<RegistrationExtensions.RegistrationExtensionsRootRegistry.AcceptanceTestsRegistry> registry) where TContext : ScenarioContext =>
        builder.CustomConfig(config =>
        {
            switch (approach)
            {
                case RegistrationApproach.Add:
                    manual(config);
                    break;
                case RegistrationApproach.Registry:
                    registry(config.Handlers.All.AcceptanceTests);
                    break;
                default:
                    throw new InvalidOperationException("Unknown approach: " + approach + "");
            }
        });
}