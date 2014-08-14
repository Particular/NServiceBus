#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using global::Ninject;

    [ObsoleteEx(Replacement = "Replace with Configure.With(c=>.UseContainer<NServiceBus.Ninject>())", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
    public static class ConfigureNinjectBuilder
    {
        [ObsoleteEx(Replacement ="Replace with Configure.With(c=>.UseContainer<NServiceBus.Ninject>())", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
        public static Configure NinjectBuilder(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Replacement ="Replace with Configure.With(c => c.UseContainer<NServiceBus.Ninject>(b => b.ExistingKernel(kernel)))", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
        public static Configure NinjectBuilder(this Configure config, IKernel kernel)
        {
            throw new NotImplementedException();
        }
    }
}