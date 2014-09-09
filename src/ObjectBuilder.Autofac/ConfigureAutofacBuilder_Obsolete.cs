#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using global::Autofac;

    [ObsoleteEx(
        Message = "Use `configuration.UseContainer<NServiceBus.Autofac>()`, where `configuration` is an instance of type `BusConfiguration`.", 
        TreatAsErrorFromVersion = "5.0", 
        RemoveInVersion = "6.0")]
    public static class ConfigureAutofacBuilder
    {
        [ObsoleteEx(
            Message = "Use `configuration.UseContainer<NServiceBus.Autofac>()`, where `configuration` is an instance of type `BusConfiguration`.", 
            TreatAsErrorFromVersion = "5.0", 
            RemoveInVersion = "6.0")]
        public static Configure AutofacBuilder(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use `configuration.UseContainer<NServiceBus.Autofac>(b => b.ExistingLifetimeScope(rootScope))`, where `configuration` is an instance of type `BusConfiguration`.", 
            TreatAsErrorFromVersion = "5.0", 
            RemoveInVersion = "6.0")]
        public static Configure AutofacBuilder(this Configure config, ILifetimeScope rootScope)
        {
            throw new NotImplementedException();
        }
    }
}