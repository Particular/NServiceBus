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
// ReSharper disable once UnusedParameter.Global
        public static Configure DefaultBuilder(this Configure config)
        {
           throw new NotImplementedException();
        }

    }
}
