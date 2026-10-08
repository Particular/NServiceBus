#nullable enable

namespace NServiceBus;

using System.Diagnostics;

static class ActivitySources
{
    public const string Version = "1.0.0";

    // The pre-v11 sources stay on 0.1.0 so existing dashboards and tests can tell the two tag and span-name
    // sets apart. Removed in v11 together with the switch, see obsoletes-v10.cs.
    internal const string PreV11Version = "0.1.0";

    // Static fields initialize in textual order, so this has to precede the sources below.
    static readonly string EffectiveVersion = V11BehaviorSwitch.UseV11Behavior ? Version : PreV11Version;

    public static readonly ActivitySource Main = new("NServiceBus.Core", EffectiveVersion);

    public static readonly ActivitySource Handler = new("NServiceBus.Core.Handler", EffectiveVersion);

    public static readonly ActivitySource Recoverability = new("NServiceBus.Core.Recoverability", EffectiveVersion);
}
