namespace NServiceBus
{
    enum SendRouteOption
    {
        None,
        ExplicitDestination,
        RouteToThisInstance,
        RouteToAnyInstanceOfThisEndpoint,
        RouteToSpecificInstance
    }
}