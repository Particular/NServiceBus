using System.Diagnostics;

namespace NServiceBus.Diagnostics;

static class ActivitySources
{
    public static readonly ActivitySource Main = new("NServiceBus.Diagnostics", "1.42.0");
}