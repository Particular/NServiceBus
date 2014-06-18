namespace NServiceBus
{
    using System;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    [ObsoleteEx(Replacement = "Configure.Serialization.Binary()", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
    public static class ConfigureBinarySerializer
    {
        /// <summary>
        /// Use binary serialization.
        /// Note that this does not support interface-based messages.
        /// </summary>
        [ObsoleteEx(Replacement = "Configure.Serialization.Binary()", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        // ReSharper disable once UnusedParameter.Global
        public static Configure BinarySerializer(this Configure config)
        {
            throw new NotImplementedException();
        }
    }
}
