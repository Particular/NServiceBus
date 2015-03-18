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
        readonly TransportReceiver[] pipelines;

        public PipelineCollection(IEnumerable<TransportReceiver> pipelines)
        {
            this.pipelines = pipelines.ToArray();
        }

        public void Start()
        {
            Parallel.ForEach(pipelines, (pipeline, state, index) =>
            {
                Logger.DebugFormat("Starting {1}/{2} {0} pipeline", pipeline.Id, index + 1, pipelines.Length);
                try
                {
                    pipeline.Start();
                    Logger.InfoFormat("Started {1}/{2} {0} pipeline", pipeline.Id, index + 1, pipelines.Length);
                }
                catch (Exception ex)
                {
                    Logger.Fatal(string.Format("Pipeline {0} failed to start.", pipeline.Id), ex);
                    throw;
                }
            });
        }

        public void Stop()
        {
            Parallel.ForEach(pipelines, (pipeline, state, index) =>
            {
                Logger.DebugFormat("Stopping {1}/{2} {0} pipeline", pipeline.Id, index + 1, pipelines.Length);
                pipeline.Stop();
                Logger.InfoFormat("Stopped {1}/{2} {0} pipeline", pipeline.Id, index + 1, pipelines.Length);
            });
        }


        static ILog Logger = LogManager.GetLogger<PipelineCollection>();
    }
}