namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    interface IPipelineExecutor
    {
        Task Invoke(MessageContext messageContext, CancellationToken cancellationToken = default);
    }
}