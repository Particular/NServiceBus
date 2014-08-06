#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using global::Autofac;

    [ObsoleteEx(Replacement = "Configure.With(c=>.UseContainer<NServiceBus.Autofac>())", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
    public static class ConfigureAutofacBuilder
    {
        [ObsoleteEx(Replacement = "Configure.With(c=>.UseContainer<NServiceBus.Autofac>())", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
        public static Configure AutofacBuilder(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Replacement = "Configure.With(c => c.UseContainer<NServiceBus.Autofac>(b => b.ExistingLifetimeScope(rootScope)))", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
        public static Configure AutofacBuilder(this Configure config, ILifetimeScope rootScope)
        {
            throw new NotImplementedException();
        }
    }
}