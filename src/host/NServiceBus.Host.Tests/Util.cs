using System.Collections.Generic;
using NServiceBus.Host.Internal;
using NServiceBus.Host.Internal.ProfileHandlers;

namespace NServiceBus.Host.Tests
{
    public static class Util
    {
        //public static Configure Init<TSpecfier>() where TSpecfier : IConfigureThisEndpoint, new()
        //{
        //    return Init<TSpecfier, ProductionProfileHandler>();
        //}

        //public static Configure Init<TSpecfier, TProfileHandler>()
        //    where TSpecfier : IConfigureThisEndpoint, new()
        //    where TProfileHandler : IHandleProfileConfiguration, new()
        //{
        //    var profile = new TProfileHandler();
        //    var specifier = new TSpecfier();

        //    profile.Init(specifier);

        //    return new ConfigurationBuilder(specifier, new List<IHandleProfileConfiguration>{ profile }).Build();
        //}
    }
}
