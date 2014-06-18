namespace NServiceBus.ObjectBuilder.Common.Config
{
    using System;

    ///<summary>
    /// Extension methods to specify a custom container type and/or instance
    ///</summary>
    public static class ConfigureContainer
    {
        [ObsoleteEx(
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5",
        Replacement = "Configure.With(c=>.UseContainer<T>())")]
// ReSharper disable once UnusedParameter.Global
        public static Configure UsingContainer<T>(this Configure configure) where T : class, IContainer, new()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(c=>.UseContainer(container))")]
        // ReSharper disable UnusedParameter.Global
        public static Configure UsingContainer<T>(this Configure configure, T container) where T : IContainer
        // ReSharper restore UnusedParameter.Global
        {
            throw new NotImplementedException();
        }
    }
}