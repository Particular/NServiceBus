namespace NServiceBus.Saga
{
    using System;

    /// <summary>
	/// An interface used to mark messages as requiring the attention of the
    /// saga infrastructure.
	/// </summary>
	[ObsoleteEx(Message = "Auto correlation for sagas are now handled by NServiceBus without the need to implement the ISagaMessage interface. You can safely remove this interface and replace it with just IMessage.", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
    public interface ISagaMessage : IMessage
    {
		/// <summary>
		/// Gets/sets the Id of the saga the message is related to.
		/// </summary>
        Guid SagaId { get; set; }
    }
}
