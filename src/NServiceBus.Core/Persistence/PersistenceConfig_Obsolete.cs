#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global

namespace NServiceBus.Persistence
{
    using System;


    public static partial class PersistenceConfig
    {
        [ObsoleteEx(Replacement = "Configure.With(c => c.UsePersistence<T>(customizations)", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure UsePersistence<T>(this Configure config, Action<PersistenceConfiguration> customizations = null) where T : PersistenceDefinition
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Replacement = "Configure.With(c => c.UsePersistence<T>(customizations)", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure UsePersistence(this Configure config, Type definitionType, Action<PersistenceConfiguration> customizations = null)
        {
            throw new NotImplementedException();
        }
    }
}