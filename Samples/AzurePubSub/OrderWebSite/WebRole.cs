using System.Collections.Generic;
using log4net.Core;
using Microsoft.WindowsAzure.ServiceRuntime;
using MyMessages;
using NServiceBus;
using NServiceBus.Config;
using NServiceBus.Integration.Azure;

public class WebRole : RoleEntryPoint
{
    public static IBus Bus;
    public static IList<MyMessages.Order> Orders;

    public override bool OnStart()
    {
        Orders = new List<MyMessages.Order>();

        ConfigureNServiceBus();
        
        //request all orders to "warmup" the cache
        Bus.Send(new LoadOrdersMessage());
        return base.OnStart();
    }

    private void ConfigureNServiceBus()
    {
        Bus = Configure.WithWeb()
            .DefaultBuilder()
            .Log4Net(new AzureAppender())
            .AzureConfigurationSource()
            .AzureMessageQueue()
            .XmlSerializer()
            .UnicastBus()
            .LoadMessageHandlers()
            .IsTransactional(true)
            //  .PurgeOnStartup(true)
            .CreateBus()
            .Start();
    }
}