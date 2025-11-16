#nullable enable

namespace NServiceBus;

using System.Diagnostics;

static class ActivitySources
{
    public static readonly ActivitySource Main =
        new("NServiceBus.Core",
            "0.1.0");
}