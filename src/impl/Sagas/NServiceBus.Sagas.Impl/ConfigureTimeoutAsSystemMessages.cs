using NServiceBus.Config.Conventions;
using NServiceBus.Saga;

namespace NServiceBus.Sagas.Impl
{
    /// <summary>
    /// Defining ITimeoutState and TimeoutMessage as valid system messages
    /// </summary>
    public class ConfigureTimeoutAsSystemMessages : IWantToRunBeforeConfiguration
    {
        /// <summary>
        /// Defining ITimeoutState and TimeoutMessage as valid system messages
        /// </summary>
        public void Init()
        {
            NServiceBus.Configure.Instance.AddSystemMessagesAs(t => typeof (ITimeoutState).IsAssignableFrom(t) || t == typeof(TimeoutMessage));
        }
    }
}
