namespace NServiceBus
{
    using System;
    using Settings;

    /// <summary>
    /// Provides a fluent api to allow the configuration of <see cref="ScaleOutSettings"/>.
    /// </summary>
    public static class ScaleOutExtentions
    {
        /// <summary>
        ///     Allows the user to control how the current endpoint behaves when scaled out.
        /// </summary>
        public static ScaleOutSettings ScaleOut(this BusConfiguration config)
        {
            return new ScaleOutSettings(config);
        }

#pragma warning disable 1591
        // ReSharper disable UnusedParameter.Global
        [ObsoleteEx(
            Message = "Use `configuration.ScaleOut().UseSingleBrokerQueue()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.", 
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
        public static Configure ScaleOut(this Configure config, Action<ScaleOutSettings> customScaleOutSettings)
        {
            throw new NotImplementedException();
        }
    }
}