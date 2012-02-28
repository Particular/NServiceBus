namespace MyServer
{
    using NServiceBus;
    using NServiceBus.Persistence.Raven;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server,IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                .DefaultBuilder()
                .RunTimeoutManager(); //will default to ravendb for storage

            //shows multi tennant operations of the sagas
            RavenSessionFactory.GetDatabaseName = (context) =>
                                                      {
                                                          if (context.Headers.ContainsKey("tennant"))
                                                              return context.Headers["tennant"];

                                                          return string.Empty;
                                                      };
        }
    }
}
