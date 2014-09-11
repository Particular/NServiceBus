#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    public static partial class ConfigureQueueCreation
    {

        [ObsoleteEx(
            Message = "Use `configuration.DoNotCreateQueues()`, where `configuration` is an instance of type `BusConfiguration`.", 
            RemoveInVersion = "6.0", 
            TreatAsErrorFromVersion = "5.0")]
        public static Configure DoNotCreateQueues(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static bool DontCreateQueues
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}