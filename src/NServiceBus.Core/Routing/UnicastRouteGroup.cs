namespace NServiceBus
{
    using Routing;

    class UnicastRouteGroup
    {
        public string EndpointName { get; }
        public UnicastRoute[] Routes { get; }

        public UnicastRouteGroup(string endpointName, UnicastRoute[] routes)
        {
            EndpointName = endpointName;
            Routes = routes;
        }
    }
}