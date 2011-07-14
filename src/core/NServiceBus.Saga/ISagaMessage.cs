using System;

namespace NServiceBus.Saga
{
	/// <summary>
	/// An interface used to mark messages as requiring the attention of the
    /// saga infrastructure.
	/// </summary>
    public interface ISagaMessage : IMessage
    {
		/// <summary>
		/// Gets/sets the Id of the saga the message is related to.
		/// </summary>
        Guid SagaId { get; set; }
    }
}
