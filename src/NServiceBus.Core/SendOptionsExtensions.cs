namespace NServiceBus.Configuration.AdvanceExtensibility
{
    using System.Collections.Generic;

    /// <summary>
    /// Extesions for advanced scenarios.
    /// </summary>
    public static class SendOptionsExtensions
    {
        /// <summary>
        /// Gets access to a "bucket", this allows the developer to pass information from extension methods down to behaviors. 
        /// </summary>
        /// <param name="options">SendOptions instance.</param>
        /// <returns>A big bucket.</returns>
        public static Dictionary<string, object> GetContext(this SendOptions options)
        {
            return options.Context;
        }

        /// <summary>
        /// Gets access to a "bucket", this allows the developer to pass information from extension methods down to behaviors. 
        /// </summary>
        /// <param name="options">SendLocalOptions instance.</param>
        /// <returns>A big bucket.</returns>
        public static Dictionary<string, object> GetContext(this SendLocalOptions options)
        {
            return options.Context;
        }
    }
}