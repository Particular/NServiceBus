namespace Receiver
{
	using NServiceBus;

	public class EndpointConfig : IConfigureThisEndpoint, AsA_Server
	{
	}

	internal class SetupDataBus : IWantCustomInitialization
	{
		public static string BasePath = "..\\..\\..\\storage";

		public void Init()
		{
		    Configure.Instance
		        .FileShareDataBus(BasePath)
		        .UnicastBus();
		}
	}
}
