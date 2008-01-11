using System.Collections.Generic;

namespace NServiceBus.Unicast.Distributor
{
    public static class Worker
    {
        public readonly static IDictionary<string, int> Threads = new Dictionary<string, int>();
    }
}
