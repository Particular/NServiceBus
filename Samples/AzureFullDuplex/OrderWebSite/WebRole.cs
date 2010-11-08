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
        
        return base.OnStart();
    }

    private void ConfigureNServiceBus()
    {
        Bus = Configure.WithWeb()
            .DefaultBuilder() // sets up the ioc container
            .Log4Net(new AzureAppender()) // logging
            .AzureConfigurationSource() // configures service configuration file support
            .AzureMessageQueue() // use azure storage queues 
                .XmlSerializer()
            .UnicastBus()
                .LoadMessageHandlers() // automatically register known message handlers
                .IsTransactional(true)
            .CreateBus()
            .Start();
    }
}