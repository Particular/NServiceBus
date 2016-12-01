using System;
using System.Collections.Generic;
using NServiceBus.Features;
using NServiceBus.Routing;
using Timer = System.Threading.Timer;

namespace NServiceBus.CentralizedRouting
{
    using Transport;
    using Unicast.Messages;

    public class CentralizedRoutingFeature : Feature
    {
        UnicastRoutingTable unicastRoutingTable;
        RoutingTable routingTable = new RoutingTable();
        CentralizedPubSubRoutingTable eventRoutingTable = new CentralizedPubSubRoutingTable();
        Timer timer;

        protected override void Setup(FeatureConfigurationContext context)
        {
            unicastRoutingTable = context.Settings.Get<UnicastRoutingTable>();

            routingTable.routingDataUpdated += RoutingTableOnRoutingDataUpdated;
            routingTable.Reload();

            var distributionPolicy = context.Settings.Get<DistributionPolicy>();
            var endpointInstances = context.Settings.Get<EndpointInstances>();
            var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();

            context.Pipeline.Replace("UnicastPublishRouterConnector", b => new CentralizedRoutingPublishRouterConnector(b.Build<MessageMetadataRegistry>(), eventRoutingTable, distributionPolicy, endpointInstances, i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i))));


            timer = new Timer(state => routingTable.Reload(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        void RoutingTableOnRoutingDataUpdated(object sender, EventArgs eventArgs)
        {
            UpdateRoutingTable(routingTable.Endpoints);
        }

        void UpdateRoutingTable(EndpointRoutingConfiguration[] endpoints)
        {
            var commandRoutes = new List<RouteTableEntry>();
            foreach (var endpoint in endpoints)
            {
                foreach (var command in endpoint.Commands)
                {
                    commandRoutes.Add(new RouteTableEntry(command,
                        UnicastRoute.CreateFromEndpointName(endpoint.LogicalEndpointName)));
                }
            }

            eventRoutingTable.ReplaceRoutes(endpoints);
            unicastRoutingTable.AddOrReplaceRoutes("FileBasedRouting", commandRoutes);
        }
    }
}