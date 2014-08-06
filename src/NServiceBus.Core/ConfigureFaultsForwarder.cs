namespace NServiceBus
{
    using System;

    /// <summary>
	/// Contains extension methods to NServiceBus.Configure
	/// </summary>
	public static class ConfigureFaultsForwarder
	{
		/// <summary>
		/// Forward messages that have repeatedly failed to another endpoint.
		/// </summary>
        [ObsoleteEx(Message="It is safe to remove this method call. This is the default behavior.", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
// ReSharper disable UnusedParameter.Global
        public static Configure MessageForwardingInCaseOfFault(this Configure config)
// ReSharper restore UnusedParameter.Global
		{
			throw new InvalidOperationException();
		}
	}
}
