#nullable enable

namespace NServiceBus;

using System;

static class AppContextSwitches
{
    enum SwitchState : byte
    {
        Unchecked = 0,
        Enabled = 1,
        Disabled = 2
    }

    static SwitchState cachedUseV2DeterministicGuid;

    public const string UseV2DeterministicGuidSwitchName = "NServiceBus.Core.Hosting.UseV2DeterministicGuid";

    public const string UsedLegacyDeterministicGuidSettingsKey = "NServiceBus.Hosting.UsedLegacyDeterministicGuid";

    public static bool UseV2DeterministicGuid
    {
        get
        {
            var state = cachedUseV2DeterministicGuid;
            if (state != SwitchState.Unchecked)
            {
                return state == SwitchState.Enabled;
            }

            state = AppContext.TryGetSwitch(UseV2DeterministicGuidSwitchName, out var isEnabled) && isEnabled
                ? SwitchState.Enabled
                : SwitchState.Disabled;
            cachedUseV2DeterministicGuid = state;

            return state == SwitchState.Enabled;
        }
    }

    internal static void ResetUseV2DeterministicGuid() => cachedUseV2DeterministicGuid = SwitchState.Unchecked;
}