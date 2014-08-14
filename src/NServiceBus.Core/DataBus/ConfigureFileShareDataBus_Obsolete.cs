#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    public static partial class ConfigureFileShareDataBus
    {

        [ObsoleteEx(Replacement = "Configure.With(c=>.FileShareDataBus(databusPath))", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure FileShareDataBus(this Configure config, string basePath)
        {
            throw new NotImplementedException();

        }

    }
}