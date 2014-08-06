#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    [ObsoleteEx(
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5",
        Message = "See http://docs.particular.net/nservicebus/how-to-specify-your-input-queue-name for how to configure the queue name.")]
    public static class ConfigureSettingLocalAddressNameAction
    {
        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Message = "See http://docs.particular.net/nservicebus/how-to-specify-your-input-queue-name for how to configure the queue name.")]
        public static Configure DefineLocalAddressNameFunc(this Configure config, Func<string> setLocalAddressNameFunc)
        {
            throw new NotImplementedException();
        }
    }
}

