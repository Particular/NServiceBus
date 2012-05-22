namespace Sender
{
	using NServiceBus;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Client, IWantCustomInitialization
	{
		public static string BasePath = "..\\..\\..\\storage";

		public void Init()
		{
		    Configure.With()
                .AutofacBuilder()
		        .FileShareDataBus(BasePath)
		        .UnicastBus();
		}
	}
}
