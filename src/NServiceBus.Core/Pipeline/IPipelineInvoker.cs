namespace NServiceBus
{
    using System.Threading.Tasks;
    using Transports;

    interface IPipelineInvoker
    {
        Task Invoke(PushContext pushContext);
    }
}