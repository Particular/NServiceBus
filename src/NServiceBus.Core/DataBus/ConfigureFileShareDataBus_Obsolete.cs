// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    public static partial class ConfigureFileShareDataBus
    {

        /// <summary>
        /// Use the file-based databus implementation with the default binary serializer.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="basePath">The location to which to write serialized properties for the databus.</param>
        /// <returns>The configuration.</returns>
        [ObsoleteEx(Replacement = "Configure.With(c=>.FileShareDataBus())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure FileShareDataBus(this Configure config, string basePath)
        {
            throw new NotImplementedException();

        }

    }
}