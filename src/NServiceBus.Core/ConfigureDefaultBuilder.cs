namespace NServiceBus
{
    using System;

    /// <summary>
    /// Configuration extension for the default builder
    /// </summary>
    public static class ConfigureDefaultBuilder
    {

       [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Message = "Default builder will be used automatically")]
#pragma warning disable 1591
// ReSharper disable once UnusedParameter.Global
        public static Configure DefaultBuilder(this Configure config)
#pragma warning restore 1591
        {
           throw new NotImplementedException();
        }

    }
}
