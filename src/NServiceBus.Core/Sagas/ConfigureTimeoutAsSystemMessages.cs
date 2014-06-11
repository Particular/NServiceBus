namespace NServiceBus.Sagas
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Saga;

    /// <summary>
    /// Defining <see cref="IHandleTimeouts{T}"/> as valid system messages
    /// </summary>
    class ConfigureTimeoutAsSystemMessages : IWantToRunBeforeConfiguration
    {
        /// <summary>
        /// Defining <see cref="IHandleTimeouts{T}"/> as valid system messages
        /// </summary>
        public void Init(Configure config)
        {
            var sagas = config.TypesToScan.Where(Features.Sagas.IsSagaType).ToList();

            config.Settings.Get<Conventions>().AddSystemMessagesConventions(t => IsTypeATimeoutHandledByAnySaga(t, sagas));
        }

        static bool IsTypeATimeoutHandledByAnySaga(Type type, IEnumerable<Type> sagas)
        {
            var timeoutHandler = typeof(IHandleTimeouts<>).MakeGenericType(type);
            var messageHandler = typeof(IHandleMessages<>).MakeGenericType(type);

            return sagas.Any(t => timeoutHandler.IsAssignableFrom(t) && !messageHandler.IsAssignableFrom(t));
        }
    }
}
