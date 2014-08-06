#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global

namespace NServiceBus
{
    using System;
        
    [ObsoleteEx(RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
	public static class ConfigureFaultsForwarder
	{
        [ObsoleteEx(Message="It is safe to remove this method call. This is the default behavior.", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure MessageForwardingInCaseOfFault(this Configure config)
		{
			throw new InvalidOperationException();
		}
	}
}
