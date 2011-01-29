namespace Sender
{
	using System;
	using NServiceBus;

	public class EndpointConfig : IConfigureThisEndpoint, AsA_Client
	{
	}

	internal class SetupDataBus : IWantCustomInitialization
	{
		public static string BasePath = "..\\..\\..\\storage";

		public void Init()
		{
			Configure.Instance.FileShareDataBus(BasePath);
		}
	}
}
