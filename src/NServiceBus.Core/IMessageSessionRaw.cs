namespace NServiceBus
{
    using System.Threading.Tasks;
    using Transport;

    /// <summary>
    /// 
    /// </summary>
    public interface IMessageSessionRaw
    {
        /// <summary>
        /// 
        /// </summary>
        Task Dispatch(params TransportOperation[] operations);
    }
}