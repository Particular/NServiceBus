#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using Castle.Windsor;

    [ObsoleteEx(Replacement = "Configure.With(c=>.UseContainer<Windsor>())", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
    public static class ConfigureWindsorBuilder
    {
        [ObsoleteEx(Replacement = "Configure.With(c=>.UseContainer<Windsor>())", TreatAsErrorFromVersion = "5.0",RemoveInVersion = "6.0")]
        public static Configure CastleWindsorBuilder(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Replacement = "Configure.With(c => c.UseContainer<Windsor>(b => b.ExistingContainer(container)))", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
        public static Configure CastleWindsorBuilder(this Configure config, IWindsorContainer container)
        {
            throw new NotImplementedException();
        }
    }
}