namespace NServiceBus
{
    using System.Diagnostics;

    static class ActivitySources
    {
        public static readonly ActivitySource Main =
            new(NServiceBusDiagnosticsInfo.InstrumentationName,
                NServiceBusDiagnosticsInfo.InstrumentationVersion);
    }
}