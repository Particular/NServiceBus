namespace NServiceBus
{
	using System;

	/// <summary>
	/// The interface used for starting and stopping an IBus.
	/// </summary>
	public static class IStartableBusExtensions
	{
		/// <summary>
		/// Starts the bus and returns a reference to it.
		/// </summary>
		/// <returns>A reference to the bus.</returns>
		[ObsoleteEx(
			TreatAsErrorFromVersion = "6",
			RemoveInVersion = "7",
			ReplacementTypeOrMember = "StartAsync()")]
		public static IBus Start(this IStartableBus bus)
		{
			throw new NotImplementedException();
		}
	}
}