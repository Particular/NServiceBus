namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Logging;

    class PipelineCollection
    {
        public PipelineCollection()
        {
        }

        public PipelineCollection(List<TransportReceiver> pipelines)
        {
            this.pipelines = pipelines;
        }

        public async Task Start()
        {
            if (pipelines == null)
            {
                return;
            }

            foreach (var pipeline in pipelines)
            {
                Logger.DebugFormat("Starting {0} pipeline", pipeline.Id);
                try
                {
                    await pipeline.Start().ConfigureAwait(false);
                    Logger.DebugFormat("Started {0} pipeline", pipeline.Id);
                }
                catch (Exception ex)
                {
                    Logger.Fatal($"Pipeline {pipeline.Id} failed to start.", ex);
                    throw;
                }
            }
        }

        public Task Stop()
        {
            if (pipelines == null)
            {
                return TaskEx.CompletedTask;
            }

            var pipelineStopTasks = pipelines.Select(async pipeline =>
            {
                Logger.DebugFormat("Stopping {0} pipeline", pipeline.Id);
                await pipeline.Stop().ConfigureAwait(false);
                Logger.DebugFormat("Stopped {0} pipeline", pipeline.Id);
            });

            return Task.WhenAll(pipelineStopTasks);
        }

        List<TransportReceiver> pipelines;


        static ILog Logger = LogManager.GetLogger<PipelineCollection>();
    }
}