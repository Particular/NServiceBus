#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    public static partial class InstallConfigExtensions
    {
        [ObsoleteEx(Replacement = "Configure.With(c => c.EnableInstallers())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure EnableInstallers(this Configure config, string username = null)
        {
            throw new InvalidOperationException();
        }
    }
}