namespace NServiceBus
{
    using System.Collections.Generic;
    using Extensibility;

    interface IUnicastRoutingTableEntry
    {
        IEnumerable<UnicastRoutingTarget> GetTargets(ContextBag contextBag);
    }
}