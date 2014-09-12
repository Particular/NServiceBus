#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global

namespace NServiceBus
{
    using System;
    using global::Spring.Context.Support;

    [ObsoleteEx(
        Message = "Replace with Use `configuration.UseContainer<SpringBuilder>()`, where `configuration` is an instance of type `BusConfiguration`.",
        TreatAsErrorFromVersion = "5.0",
        RemoveInVersion = "6.0")]
    public static class ConfigureSpringBuilder
    {
        [ObsoleteEx(
            Message = "Use `configuration.UseContainer<SpringBuilder>()`, where `configuration` is an instance of type `BusConfiguration`.",
            TreatAsErrorFromVersion = "5.0",
            RemoveInVersion = "6.0")]
        public static Configure SpringFrameworkBuilder(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use `configuration.UseContainer<SpringBuilder>(b => b.ExistingApplicationContext(applicationContext))`, where `configuration` is an instance of type `BusConfiguration`.",
            TreatAsErrorFromVersion = "5.0",
            RemoveInVersion = "6.0")]
        public static Configure SpringFrameworkBuilder(this Configure config, GenericApplicationContext applicationContext)
        {
            throw new NotImplementedException();
        }

    }
}