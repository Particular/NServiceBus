#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global

namespace NServiceBus.ObjectBuilder.Common.Config
{
    using System;

    [ObsoleteEx(
        RemoveInVersion = "6",
        TreatAsErrorFromVersion = "5",
        Replacement = "Configure.With(c=>c.UseContainer<T>())")]
    public static class ConfigureContainer
    {
        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(c=>c.UseContainer<T>())")]
        public static Configure UsingContainer<T>(this Configure configure) where T : class, IContainer, new()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Replacement = "Configure.With(c=>c.UseContainer(container))")]
        public static Configure UsingContainer<T>(this Configure configure, T container) where T : IContainer
        {
            throw new NotImplementedException();
        }
    }
}