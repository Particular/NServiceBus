namespace NServiceBus.AcceptanceTesting;

using System;
using System.Collections.Generic;
using Features;

/// <summary>
/// Simple feature that allows registration of <see cref="FeatureStartupTask"/> without having to define a <see cref="Feature"/> beforehand.
/// </summary>
sealed class FeatureStartupTaskRunner : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
        foreach (var startupTaskRegistration in context.Settings.GetOrDefault<List<Action<FeatureConfigurationContext>>>() ?? [])
        {
            startupTaskRegistration(context);
        }
    }
}