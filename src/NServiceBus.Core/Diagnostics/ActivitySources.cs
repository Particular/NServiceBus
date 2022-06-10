namespace NServiceBus
{
    using System.Diagnostics;

    static class NServiceBusDiagnosticsInfo
    {
        // TODO: Should we re-use the name NServiceBus.Extensions.Diagnostics for minimal migration effort?
        public static string InstrumentationName = "NServiceBus.Diagnostics";

        // TODO: Set a real version
        public static string InstrumentationVersion = "1.42.0";
    }

    static class ActivitySources
    {
        public static readonly ActivitySource Main =
            new(NServiceBusDiagnosticsInfo.InstrumentationName,
                NServiceBusDiagnosticsInfo.InstrumentationVersion);
    }
}