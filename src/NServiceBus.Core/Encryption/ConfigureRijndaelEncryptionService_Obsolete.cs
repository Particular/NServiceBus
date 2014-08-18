#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    public static partial class ConfigureRijndaelEncryptionService
    {
        [ObsoleteEx(Replacement = "Configure.With(c=>c.RijndaelEncryptionService())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure RijndaelEncryptionService(this Configure config)
        {
            throw new NotImplementedException();
        }

    }
}
