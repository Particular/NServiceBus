#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using Castle.Windsor;

    [ObsoleteEx(
        Message = "Use `configuration.UseContainer<NServiceBus.Windsor>()`, where `configuration` is an instance of type `BusConfiguration`.", 
        TreatAsErrorFromVersion = "5.0", 
        RemoveInVersion = "6.0")]
    public static class ConfigureWindsorBuilder
    {
        [ObsoleteEx(
            Message = "Use `configuration.UseContainer<NServiceBus.Windsor>()`, where` configuration` is an instance of type BusConfiguration`.", 
            TreatAsErrorFromVersion = "5.0",
            RemoveInVersion = "6.0")]
        public static Configure CastleWindsorBuilder(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use `configuration.UseContainer<NServiceBus.Windsor>(b => b.ExistingContainer(container))`, where `configuration` is an instance of type `BusConfiguration`.", 
            TreatAsErrorFromVersion = "5.0",
            RemoveInVersion = "6.0")]
        public static Configure CastleWindsorBuilder(this Configure config, IWindsorContainer container)
        {
            throw new NotImplementedException();
        }
    }
}