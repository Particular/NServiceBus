namespace NServiceBus
{
    using System.Collections.Generic;
    using Extensibility;

    class SingleUnicastRoutingTableEntry : IUnicastRoutingTableEntry
    {
        UnicastRoutingTarget target;

        public SingleUnicastRoutingTableEntry(UnicastRoutingTarget target)
        {
            this.target = target;
        }

        public IEnumerable<UnicastRoutingTarget> GetTargets(ContextBag contextBag)
        {
            yield return target;
        }

        public override string ToString()
        {
            return target.ToString();
        }
    }
}