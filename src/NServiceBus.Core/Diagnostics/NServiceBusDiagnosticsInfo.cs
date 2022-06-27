namespace NServiceBus;

static class NServiceBusDiagnosticsInfo
{
    //TODO should we use a "default" namespace to allow easier separation with verbose/advanced namespaces when using wildcards? (e.g. NServiceBus.*, NServiceBus.Default.*, NServiceBus.Verbose.*)
    public static string InstrumentationName = "NServiceBus.Core";

    public static string InstrumentationVersion = "0.1.0";
}