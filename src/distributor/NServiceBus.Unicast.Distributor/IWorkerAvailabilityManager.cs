using System;
using System.Collections.Generic;
using System.Text;

namespace NServiceBus.Unicast.Distributor
{
    public interface IWorkerAvailabilityManager
    {
        void WorkerAvailable(string address);

        string PopAvailableWorker();

        void ClearAvailabilityForWorker(string address);
    }
}
