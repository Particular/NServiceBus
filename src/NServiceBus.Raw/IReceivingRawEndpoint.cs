using System.Threading.Tasks;

namespace NServiceBus.Raw
{
    /// <summary>
    /// Represents an endpoint in the running phase.
    /// </summary>
    public interface IReceivingRawEndpoint : IStoppableRawEndpoint, IRawEndpoint
    {
        /// <summary>
        /// Stops receiving of messages. The endpoint can still send messages.
        /// </summary>
        Task<IStoppableRawEndpoint> StopReceiving();
    }
}