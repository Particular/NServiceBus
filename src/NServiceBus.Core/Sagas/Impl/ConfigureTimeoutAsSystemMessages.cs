namespace NServiceBus.Sagas.Impl
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
            var sagas = NServiceBus.Configure.TypesToScan.Where(Configure.IsSagaType).ToList();

            NServiceBus.Configure.Instance.AddSystemMessagesAs(t => IsTypeATimeoutHandledByAnySaga(t, sagas));
        }

        bool IsTypeATimeoutHandledByAnySaga(Type type, IEnumerable<Type> sagas)
        {
            var timeoutHandler = typeof(IHandleTimeouts<>).MakeGenericType(type);

            return sagas.Any(timeoutHandler.IsAssignableFrom);
        }
    }
}
