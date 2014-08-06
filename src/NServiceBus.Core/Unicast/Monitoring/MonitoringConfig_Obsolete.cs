
#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    public static partial class MonitoringConfig
    {
        [ObsoleteEx(Replacement = "Configure.With(c=>.EnablePerformanceCounters(TimeSpan))", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure SetEndpointSLA(this Configure config, TimeSpan sla)
        {
            throw new NotImplementedException();
        }


        [ObsoleteEx(Replacement = "Configure.With(c=>.EnablePerformanceCounters())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure EnablePerformanceCounters(this Configure config)
        {
            throw new NotImplementedException();
        }


    }
}