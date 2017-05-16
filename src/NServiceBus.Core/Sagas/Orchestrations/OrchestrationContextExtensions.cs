namespace NServiceBus.Sagas.Orchestrations
{
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods.
    /// </summary>
    public static class OrchestrationContextExtensions
    {
        /// <summary>
        /// Sends a request message.
        /// </summary>
        /// <returns>Returns a task that is a promise of the reply</returns>
        public static async Task<TReply> Exec<TReply>(this IOrchestrationContext context, IRequest<TReply> request)
        {
            return (TReply) await context.ExecRaw(request).ConfigureAwait(false);
        }
    }
}