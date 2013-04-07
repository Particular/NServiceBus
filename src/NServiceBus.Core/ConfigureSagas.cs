﻿namespace NServiceBus
{
    using Config;

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
        public static Configure Sagas(this Configure config)
        {
            Feature.Enable<Features.Sagas>();

            NServiceBus.Sagas.Impl.Configure
                .With(config.Configurer, config.Builder)
                .SagasIn(Configure.TypesToScan);

            return config;
        }
    }

   
   
}