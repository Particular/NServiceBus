using NServiceBus.Config.Conventions;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Unicast.BackwardCompatibility
{
    /// <summary>
    /// Register CompletionMessage as system message 
    /// </summary>
    public class ConfigureBackwardCompatilibity : IWantToRunBeforeConfiguration
    {
        /// <summary>
        /// Register CompletionMessage as system message
        /// </summary>
        public void Init()
        {
            Configure.Instance.AddSystemMessagesAs(t => t == typeof(CompletionMessage));
        }
    }
}
