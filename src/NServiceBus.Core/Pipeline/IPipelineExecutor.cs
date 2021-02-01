namespace NServiceBus
{
    using System.Threading.Tasks;
    using Transport;

    interface IPipelineExecutor
    {
        Task<MessageProcessingResult> Invoke(MessageContext messageContext);
    }
}