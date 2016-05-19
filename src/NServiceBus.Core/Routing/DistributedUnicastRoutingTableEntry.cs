namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Extensibility;

    class DistributedUnicastRoutingTableEntry : IUnicastRoutingTableEntry
    {
        Func<ContextBag, IEnumerable<UnicastRoutingTarget>> distributionFunction;
        List<UnicastRoutingTarget> targets;

        public DistributedUnicastRoutingTableEntry(Func<ContextBag, IEnumerable<UnicastRoutingTarget>> distributionFunction, List<UnicastRoutingTarget> targets)
        {
            this.distributionFunction = distributionFunction;
            this.targets = targets;
        }

        public IEnumerable<UnicastRoutingTarget> GetTargets(ContextBag contextBag)
        {
            return distributionFunction(contextBag);
        }

        public override string ToString()
        {
            return string.Join(",", targets.Select(t => t.ToString()));
        }
    }
}