#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global

namespace NServiceBus.ObjectBuilder.Common.Config
{
    using System;

    [ObsoleteEx(
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5",
        Replacement = "Use configuration.UseContainer<T>(), where configuration is an instance of type BusConfiguration")]
    public static class ConfigureContainer
    {
        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Use configuration.UseContainer<T>(), where configuration is an instance of type BusConfiguration")]
        public static Configure UsingContainer<T>(this Configure configure) where T : class, IContainer, new()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Use configuration.UseContainer(container), where configuration is an instance of type BusConfiguration")]
        public static Configure UsingContainer<T>(this Configure configure, T container) where T : IContainer
        {
            throw new NotImplementedException();
        }
    }
}