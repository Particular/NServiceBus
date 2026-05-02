#nullable enable

namespace NServiceBus;

using System;
using Particular.Obsoletes;

static class AppContextSwitches
{
    enum SwitchState : byte
    {
        Unchecked = 0,
        Enabled = 1,
        Disabled = 2
    }

    static SwitchState cachedUseV2DeterministicGuid;

    [PreObsolete("https://github.com/Particular/NServiceBus/issues/7734",
        Note = "In v11, DeterministicGuid (XxHash128) becomes the default and this switch will be inverted so that setting it to false opts into the legacy MD5 algorithm. Both the switch and LegacyDeterministicGuid will be removed in v12.",
        ReplacementTypeOrMember = "DeterministicGuid")]
    public const string UseV2DeterministicGuidSwitchName = "NServiceBus.Core.Hosting.UseV2DeterministicGuid";

    public const string UsedLegacyDeterministicGuidSettingsKey = "NServiceBus.Hosting.UsedLegacyDeterministicGuid";

    [PreObsolete("https://github.com/Particular/NServiceBus/issues/7734",
        Note = "In v11, this property will be removed or replaced by one that inverts the semantics (defaulting to true). Setting the switch to false will opt into LegacyDeterministicGuid, which is itself marked for removal in v12.",
        ReplacementTypeOrMember = "DeterministicGuid")]
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