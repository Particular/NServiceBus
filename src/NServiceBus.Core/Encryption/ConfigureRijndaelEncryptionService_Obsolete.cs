// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static partial class ConfigureRijndaelEncryptionService
    {
        /// <summary>
        /// Use 256 bit AES encryption based on the Rijndael cipher. 
        /// </summary>
        [ObsoleteEx(Replacement = "Configure.With(c=>.RijndaelEncryptionService())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure RijndaelEncryptionService(this Configure config)
        {
            throw new NotImplementedException();
        }

    }
}
