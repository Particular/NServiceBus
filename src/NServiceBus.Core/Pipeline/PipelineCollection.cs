namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using NServiceBus.Unicast.Transport;

    class PipelineCollection
    {
        TransportReceiver[] pipelines;

        public PipelineCollection(IEnumerable<TransportReceiver> pipelines)
        {
            this.pipelines = pipelines.ToArray();
        }

        public async Task Start()
        {
            foreach (var pipeline in pipelines)
            {
                Logger.DebugFormat("Starting {0} pipeline", pipeline.Id);
                try
                {
                    await pipeline.Start().ConfigureAwait(false);
                    Logger.InfoFormat("Started {0} pipeline", pipeline.Id);
                }
                catch (Exception ex)
                {
                    Logger.Fatal(string.Format("Pipeline {0} failed to start.", pipeline.Id), ex);
                    throw;
                }
            }
        }

        public async Task Stop()
        {
            foreach (var pipeline in pipelines)
            {
                Logger.DebugFormat("Stopping {0} pipeline", pipeline.Id);
                await pipeline.Stop().ConfigureAwait(false);
                Logger.InfoFormat("Stopped {0} pipeline", pipeline.Id);
            }
        }


        static ILog Logger = LogManager.GetLogger<PipelineCollection>();
    }
}