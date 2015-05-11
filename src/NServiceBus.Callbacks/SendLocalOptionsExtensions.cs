namespace NServiceBus
{
    using System.Threading;

    /// <summary>
    /// Extensions to the send local options
    /// </summary>
    public static class SendLocalOptionsExtensions
    {
        /// <summary>
        /// Registers a cancellation token on the send local options
        /// </summary>
        /// <param name="options">The send local options</param>
        /// <param name="cancellationToken">The cancellation token which allows to cancel the response task.</param>
        /// <returns>The send local options</returns>
        public static SendLocalOptions RegisterCancellationToken(this SendLocalOptions options, CancellationToken cancellationToken)
        {
            options.RegisterTokenInternal(cancellationToken);
            return options;
        }
    }
}