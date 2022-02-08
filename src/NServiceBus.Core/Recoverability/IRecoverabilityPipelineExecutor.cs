namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    interface IRecoverabilityPipelineExecutor
    {
        Task<ErrorHandleResult> Invoke(ErrorContext errorContext, CancellationToken cancellationToken = default);
    }
}