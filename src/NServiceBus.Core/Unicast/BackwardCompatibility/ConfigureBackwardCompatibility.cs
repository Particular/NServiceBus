namespace NServiceBus.Unicast.BackwardCompatibility
{
    using NServiceBus.Config.Conventions;
    using Transport;

    /// <summary>
    /// Register CompletionMessage as system message 
    /// </summary>
    public class ConfigureBackwardCompatibility : IWantToRunBeforeConfiguration
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
