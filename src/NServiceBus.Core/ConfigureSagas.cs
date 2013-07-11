﻿namespace NServiceBus
{
    using Features;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureSagas
    {
        /// <summary>
        /// Configure this endpoint to support sagas.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [ObsoleteEx(Replacement = "Configure.Features.Enable<Sagas>()", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]    
        public static Configure Sagas(this Configure config)
        {
            Feature.Enable<Features.Sagas>();
            return config;
        }
    }

   
   
}