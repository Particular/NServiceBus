namespace NServiceBus.PowerShell
{
    using System.Management.Automation;
    using System.Xml.Linq;

    [Cmdlet(VerbsCommon.Add, "NServiceBusNHibernateConfig")]
    public class AddNHibernateConfig : AddConfigSection
    {
        private const string Instructions =
            @"
To run NServiceBus with NHibernate you need to at least specify the database connectionstring.
Here is an example of what is required:
  <appSettings>
    <!-- dialect is defaulted to MsSql2008Dialect, if needed change accordingly -->
    <add key=""NServiceBus/Persistence/NHibernate/dialect"" value=""NHibernate.Dialect.{your dialect}""/>

    <!-- other optional settings examples -->
    <add key=""NServiceBus/Persistence/NHibernate/connection.provider"" value=""NHibernate.Connection.DriverConnectionProvider""/>
    <add key=""NServiceBus/Persistence/NHibernate/connection.driver_class"" value=""NHibernate.Driver.Sql2008ClientDriver""/>
    <!-- For more setting see http://www.nhforge.org/doc/nh/en/#configuration-hibernatejdbc and http://www.nhforge.org/doc/nh/en/#configuration-optional -->
  </appSettings>
  
  <connectionStrings>
    <!-- Default connection string for all Nhibernate/Sql persisters -->
    <add name=""NServiceBus/Persistence"" connectionString=""Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True"" />
    
    <!-- Optional overrides per persister -->
    <add name=""NServiceBus/Persistence/NHibernate/Timeout"" connectionString=""Data Source=.\SQLEXPRESS;Initial Catalog=timeout;Integrated Security=True"" />
    <add name=""NServiceBus/Persistence/NHibernate/Saga"" connectionString=""Data Source=.\SQLEXPRESS;Initial Catalog=sagas;Integrated Security=True"" />
    <add name=""NServiceBus/Persistence/NHibernate/Subscription"" connectionString=""Data Source=.\SQLEXPRESS;Initial Catalog=subscription;Integrated Security=True"" />
    <add name=""NServiceBus/Persistence/NHibernate/Gateway"" connectionString=""Data Source=.\SQLEXPRESS;Initial Catalog=gateway;Integrated Security=True"" />
    <add name=""NServiceBus/Persistence/NHibernate/Distributor"" connectionString=""Data Source=.\SQLEXPRESS;Initial Catalog=distributor;Integrated Security=True"" />
  </connectionStrings>";
        
        public override void ModifyConfig(XDocument doc)
        {
            doc.Root.LastNode.AddAfterSelf(new XComment(Instructions));
        }
    }
}