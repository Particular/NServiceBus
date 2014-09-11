#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using global::Ninject;

    [ObsoleteEx(
        Message = "Use `configuration.UseContainer<NServiceBus.Ninject>()`, where `configuration` is an instance of type `BusConfiguration`.",
        TreatAsErrorFromVersion = "5.0",
        RemoveInVersion = "6.0")]
    public static class ConfigureNinjectBuilder
    {
        [ObsoleteEx(
            Message = "Use `configuration.UseContainer<NServiceBus.Ninject>()`, where `configuration` is an instance of type `BusConfiguration`.", 
            TreatAsErrorFromVersion = "5.0",
            RemoveInVersion = "6.0")]
        public static Configure NinjectBuilder(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use `configuration.UseContainer<NServiceBus.Ninject>(b => b.ExistingKernel(kernel))`, where `configuration` is an instance of type `BusConfiguration`.",
            TreatAsErrorFromVersion = "5.0",
            RemoveInVersion = "6.0")]
        public static Configure NinjectBuilder(this Configure config, IKernel kernel)
        {
            throw new NotImplementedException();
        }
    }
}