namespace NServiceBus.Sagas
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Saga;

    /// <summary>
    /// Defining <see cref="IHandleTimeouts{T}"/> as valid system messages
    /// </summary>
    public class ConfigureTimeoutAsSystemMessages : IWantToRunBeforeConfiguration
    {
        /// <summary>
        /// Defining <see cref="IHandleTimeouts{T}"/> as valid system messages
        /// </summary>
        public void Init()
        {
            var sagas = Configure.TypesToScan.Where(Features.Sagas.IsSagaType).ToList();

            Configure.Instance.AddSystemMessagesAs(t => IsTypeATimeoutHandledByAnySaga(t, sagas));
        }

        static bool IsTypeATimeoutHandledByAnySaga(Type type, IEnumerable<Type> sagas)
        {
            var timeoutHandler = typeof(IHandleTimeouts<>).MakeGenericType(type);
            var messageHandler = typeof(IHandleMessages<>).MakeGenericType(type);

            return sagas.Any(t => timeoutHandler.IsAssignableFrom(t) && !messageHandler.IsAssignableFrom(t));
        }
    }
}
