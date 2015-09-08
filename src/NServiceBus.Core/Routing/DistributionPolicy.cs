namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Allows to configure message distribution strategies.
    /// </summary>
    public class DistributionPolicy
    {
        List<Tuple<Func<Type, bool>,DistributionStrategy>> strategies = new List<Tuple<Func<Type, bool>, DistributionStrategy>>();

        /// <summary>
        /// Creates a new distribution policy object.
        /// </summary>
        public DistributionPolicy()
        {
            strategies.Add(new Tuple<Func<Type, bool>, DistributionStrategy>(_ => true, new SingleInstanceRoundRobinDistributionStrategy()));
        }

        /// <summary>
        /// Sets a distribution strategy for a given subset of message types.
        /// </summary>
        /// <param name="distributionStrategy">The instance of a distribution strategy.</param>
        /// <param name="typeMatchingRule">A predicate for determining the set of types.</param>
        public void SetDistributionStrategy(DistributionStrategy distributionStrategy, Func<Type, bool> typeMatchingRule)
        {
            strategies.Insert(0, Tuple.Create(typeMatchingRule, distributionStrategy));
        }

        internal DistributionStrategy GetDistributionStrategy(Type messageType)
        {
            return strategies.Where(s => s.Item1(messageType)).Select(s => s.Item2).FirstOrDefault();
        }
    }
}