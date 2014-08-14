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
        public static void ScaleOut(this ConfigurationBuilder config, Action<ScaleOutSettings> customScaleOutSettings)
        {
            customScaleOutSettings(new ScaleOutSettings(config));
        }

#pragma warning disable 1591
        // ReSharper disable UnusedParameter.Global
        [ObsoleteEx(Replacement = "Configure.With(b => b.ScaleOut(s => s.UseSingleBrokerQueue()))", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure ScaleOut(this Configure config, Action<ScaleOutSettings> customScaleOutSettings)
        {
            throw new NotImplementedException();
        }
    }
}