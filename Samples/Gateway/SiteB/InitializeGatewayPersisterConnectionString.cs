namespace SiteB
{
    /*
     *  Uncomment the following to run the gateway with SQLServer persister
     *  Install the SQL gateway schema at your database: https://github.com/NServiceBus/NServiceBus/blob/develop/src/gateway/NServiceBus.Gateway/Persistence/Sql/Schema.sql
     *  Change the connection string
     */
    //public class InitializeGatewayPersisterConnectionString : NServiceBus.INeedInitialization
    //{
    //    public void Init()
    //    {
    //        NServiceBus.Configure.Instance.Configurer
    //            .ConfigureProperty<NServiceBus.Gateway.Persistence.Sql.SqlPersistence>(
    //                p => p.ConnectionString, @"Data Source=localhost\SQLEXPRESS;Initial Catalog=SiteB;Integrated Security=True;Pooling=False");
    //    }
    //}
}