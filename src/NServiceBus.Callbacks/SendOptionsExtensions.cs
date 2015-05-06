namespace NServiceBus
{
    using System.Threading;

    public static class SendOptionsExtensions
    {
        /// <summary>
        /// Registers a cancellation token on the send options
        /// </summary>
        /// <param name="options">The send options</param>
        /// <param name="cancellationToken">The cancellation token which allows to cancel the response task.</param>
        /// <returns>The send options</returns>
        public static SendOptions RegisterToken(this SendOptions options, CancellationToken cancellationToken)
        {
            options.RegisterTokenInternal(cancellationToken);
            return options;
        }
    }
}