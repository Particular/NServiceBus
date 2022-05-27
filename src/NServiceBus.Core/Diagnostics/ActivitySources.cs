namespace NServiceBus.Diagnostics
{
    using System.Diagnostics;

    static class ActivitySources
    {
        // TODO: Set a real version
        // TODO: Should we re-use the name NServiceBus.Extensions.Diagnostics for minimal migration effort?
        public static readonly ActivitySource Main = new("NServiceBus.Diagnostics", "1.42.0");
    }
}