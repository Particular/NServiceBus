namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Routing;

    class DistributionPolicy
    {
        public DistributionPolicy()
        {
            strategies.Add(new Tuple<Func<Type, bool>, DistributionStrategy>(_ => true, new SingleInstanceRoundRobinDistributionStrategy()));
        }

        internal void SetDistributionStrategy(DistributionStrategy distributionStrategy, Func<Type, bool> typeMatchingRule)
        {
            strategies.Insert(0, Tuple.Create(typeMatchingRule, distributionStrategy));
        }

        internal DistributionStrategy GetDistributionStrategy(Type messageType)
        {
            return strategies.Where(s => s.Item1(messageType)).Select(s => s.Item2).FirstOrDefault();
        }

        List<Tuple<Func<Type, bool>, DistributionStrategy>> strategies = new List<Tuple<Func<Type, bool>, DistributionStrategy>>();
    }
}