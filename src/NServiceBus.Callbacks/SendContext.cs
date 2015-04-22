namespace NServiceBus
{
    using System.Threading.Tasks;

    /// <summary>
    /// Synchronous request/response context.
    /// </summary>
    public class SendContext<TResponse>
    {
        readonly TaskCompletionSource<TResponse> taskCompletionSource;

        /// <summary>
        /// Creates a new instance of <see cref="SendContext{TResponse}"/>.
        /// </summary>
        /// <param name="taskCompletionSource">The <see cref="TaskCompletionSource{TResponse}"/> to set with the reply.</param>
        public SendContext(TaskCompletionSource<TResponse> taskCompletionSource)
        {
            this.taskCompletionSource = taskCompletionSource;
        }

        /// <summary>
        /// The response <see cref="Task"/>.
        /// </summary>
        public Task<TResponse> ResponseTask { get { return taskCompletionSource.Task; } }
    }
}