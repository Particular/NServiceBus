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
            Replacement = "Just remove, builder is defaulted if needed in v5")]
// ReSharper disable once UnusedParameter.Global
        public static Configure DefaultBuilder(this Configure config)
        {
           throw new NotImplementedException();
        }

    }
}
