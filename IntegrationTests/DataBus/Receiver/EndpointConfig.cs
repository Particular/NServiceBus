namespace Receiver
{
	using NServiceBus;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
	{
        public static string BasePath = "..\\..\\..\\storage";

		public void Init()
		{
		    Configure.With()
                .NinjectBuilder()
		        .FileShareDataBus(BasePath)
		        .UnicastBus();
		}
	}
}
