using System;

namespace NServiceBus.Saga
{
	/// <summary>
	/// An interface used to mark messages as requiring the attention of the
    /// saga infrastructure.
	/// </summary>
	[Obsolete("Auto correlation for sagas are now handled by NServiceBus without the need to implement the ISagaMessage interface. You can safely remove this interface",false)]
    public interface ISagaMessage : IMessage
    {
		/// <summary>
		/// Gets/sets the Id of the saga the message is related to.
		/// </summary>
        Guid SagaId { get; set; }
    }
}
