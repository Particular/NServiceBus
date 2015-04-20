namespace NServiceBus
{
    using System.Threading.Tasks;

    /// <summary>
    /// 
    /// </summary>
    public class SendContext<TResponse>
    {
        readonly TaskCompletionSource<TResponse> taskCompletionSource;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="taskCompletionSource"></param>
        public SendContext(TaskCompletionSource<TResponse> taskCompletionSource)
        {
            this.taskCompletionSource = taskCompletionSource;
        }

        /// <summary>
        /// 
        /// </summary>
        public Task<TResponse> ResponseTask { get { return taskCompletionSource.Task; } }
    }
}