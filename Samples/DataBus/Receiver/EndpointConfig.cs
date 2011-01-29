namespace Receiver
{
	using System;
	using NServiceBus;

	public class EndpointConfig:IConfigureThisEndpoint,AsA_Server
	{
	
	}

	public class SetupDataBus : IWantCustomInitialization
	{
		public static string BasePath = "..\\..\\..\\storage";

		public void Init()
		{
			Configure.Instance.FileShareDataBus(BasePath);
		}
	}

	
}
