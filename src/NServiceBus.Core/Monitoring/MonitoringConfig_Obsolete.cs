#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global

namespace NServiceBus
{
    using System;

    [ObsoleteEx(Replacement = "Configure.With(c=>.EnableCriticalTime()) or Configure.With(c=>.EnableSla(TimeSpan))", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
    public static class MonitoringConfig
    {
        [ObsoleteEx(Replacement = "Configure.With(c=>.EnableSla(TimeSpan))", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure SetEndpointSLA(this Configure config, TimeSpan sla)
        {
            throw new NotImplementedException();
        }


        [ObsoleteEx(Replacement = "Configure.With(c=>.EnableCriticalTime())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure EnablePerformanceCounters(this Configure config)
        {
            throw new NotImplementedException();
        }


    }
}