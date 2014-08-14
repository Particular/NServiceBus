#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using ObjectBuilder.Common;

    [ObsoleteEx(Replacement ="Configure.With(c=>.UseContainer<NServiceBus.StructureMap>())", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
    public static class ConfigureStructureMapBuilder
    {
        [ObsoleteEx(Replacement ="Configure.With(c=>.UseContainer<NServiceBus.StructureMap>())", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
        public static Configure StructureMapBuilder(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Replacement = "Configure.With(c => c.UseContainer<NServiceBus.StructureMap>(b => b.ExistingContainer(container)))", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
        public static Configure StructureMapBuilder(this Configure config, IContainer container)
        {
            throw new NotImplementedException();

        }
    }
}

