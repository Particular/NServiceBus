namespace NServiceBus.Diagnostics
{
    using System.Diagnostics;

    static class ActivitySources
    {
        public static readonly ActivitySource Main = new("NServiceBus.Diagnostics", "1.42.0");
    }
}