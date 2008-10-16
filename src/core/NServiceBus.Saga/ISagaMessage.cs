using System;

namespace NServiceBus.Saga
{
	/// <summary>
	/// A marker interface that also defines the properties of a
	/// message involved in an NServiceBus workflow.
	/// </summary>
    public interface ISagaMessage : IMessage
    {
		/// <summary>
		/// Gets/sets the Id of the workflow the message is related to.
		/// </summary>
        Guid SagaId { get; set; }
    }

	/// <summary>
	/// Indicates that the object starts a workflow.
	/// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class StartsSagaAttribute : Attribute
    {
    }
}
