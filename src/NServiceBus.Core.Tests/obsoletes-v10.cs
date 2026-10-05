namespace NServiceBus.Core.Tests;

using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

// =============================================================================
// EVERYTHING IN THIS FILE IS TEMPORARY AND WILL BE REMOVED IN v11, together with
// the V11BehaviorSwitch block in NServiceBus.Core/obsoletes-v10.cs.
// =============================================================================

// Runs the test with the NServiceBus.Core.OpenTelemetry.UseV11Behavior AppContext switch enabled, i.e. with
// the OpenTelemetry defaults of v11. In v11 these defaults are the only behavior: delete this attribute and
// remove it from the tests that use it.
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true)]
public sealed class OpenTelemetryV11DefaultsAttribute : Attribute, ITestAction
{
    public ActionTargets Targets => ActionTargets.Test;

    public void BeforeTest(ITest test) => Set(true);

    public void AfterTest(ITest test) => Set(false);

    static void Set(bool enabled)
    {
        AppContext.SetSwitch(V11BehaviorSwitch.UseV11BehaviorSwitchName, enabled);
        V11BehaviorSwitch.ResetUseV11Behavior();
    }
}
