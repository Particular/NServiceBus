namespace NServiceBus
{
    using System.Threading.Tasks;

    interface IPipelineBase<T>
    {
        Task Invoke(T context);
    }
}