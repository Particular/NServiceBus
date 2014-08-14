namespace NServiceBus
{
    using System;

    /// <summary>
    ///     Configure Extensions.
    /// </summary>
    public static class ConfigureExtensions
    {
        /// <summary>
        ///     Configures this endpoint as a send only endpoint.
        /// </summary>
        /// <remarks>
        ///     Use this in endpoints whose only purpose is sending messages, websites are often a good example of send only endpoints.
        /// </remarks>
        [ObsoleteEx(Replacement = "Configure.With(c => c.SendOnly())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
// ReSharper disable UnusedParameter.Global
        public static IBus SendOnly(this Configure config)
// ReSharper restore UnusedParameter.Global
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        ///     Configures this endpoint as a send only endpoint.
        /// </summary>
        /// <remarks>
        ///     Use this in endpoints whose only purpose is sending messages, websites are often a good example of send only endpoints.
        /// </remarks>
        public static void SendOnly(this ConfigurationBuilder config)
        {
            config.settings.Set("Endpoint.SendOnly", true);
        }
    }
}