using NServiceBus;

namespace Subscriber1
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using NServiceBus.AutomaticSubscriptions;

    class EndpointConfig : IConfigureThisEndpoint, AsA_Server,IWantCustomInitialization
    {
        public void Init()
        {
            //uncomment the line below if you want to use a custom auto subscription strategy
            //Configure.Features.AutoSubscribe(f => f.CustomAutoSubscriptionStrategy<MyAutoSub>());

            Configure.With()
                //this overrides the NServiceBus default convention of IEvent
                .DefiningEventsAs(t => t.Namespace != null && t.Namespace.StartsWith("MyMessages"));}
    }

    public class MyAutoSub : IAutoSubscriptionStrategy
    {
        public IEnumerable<Type> GetEventsToSubscribe()
        {
            return new BindingList<Type>();
        }
    }
}