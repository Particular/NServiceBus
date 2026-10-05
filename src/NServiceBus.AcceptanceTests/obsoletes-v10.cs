namespace NServiceBus.AcceptanceTests;

using System;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

// =============================================================================
// EVERYTHING IN THIS FILE IS TEMPORARY AND WILL BE REMOVED IN v11, together with
// the V11BehaviorSwitch block in NServiceBus.Core/obsoletes-v10.cs. The file is
// excluded from the shipped acceptance test sources in the project file.
// =============================================================================

// Runs the test with the NServiceBus.Core.OpenTelemetry.UseV11Behavior AppContext switch enabled, i.e. with
// the OpenTelemetry defaults of v11. The switch value is cached process-wide, so the cache is reset through
// the internal V11BehaviorSwitch.ResetUseV11Behavior method. Only safe in [NonParallelizable] fixtures such
// as OpenTelemetryAcceptanceTest. In v11 these defaults are the only behavior: delete this attribute and
// remove it from the tests that use it.
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true)]
public sealed class OpenTelemetryV11DefaultsAttribute : Attribute, ITestAction
{
    const string SwitchName = "NServiceBus.Core.OpenTelemetry.UseV11Behavior";

    static readonly MethodInfo ResetSwitch = typeof(EndpointConfiguration).Assembly
        .GetType("NServiceBus.V11BehaviorSwitch", throwOnError: true)!
        .GetMethod("ResetUseV11Behavior", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("NServiceBus.V11BehaviorSwitch.ResetUseV11Behavior not found");

    public ActionTargets Targets => ActionTargets.Test;

    public void BeforeTest(ITest test) => Set(true);

    public void AfterTest(ITest test) => Set(false);

    static void Set(bool enabled)
    {
        AppContext.SetSwitch(SwitchName, enabled);
        ResetSwitch.Invoke(null, null);
    }
}
