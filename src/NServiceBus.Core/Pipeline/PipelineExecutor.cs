namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using System.Linq;
    using Contexts;
    using ObjectBuilder;
    using Settings;

    /// <summary>
    ///     Orchestrates the execution of a pipeline.
    /// </summary>
    public class PipelineExecutor
    {
        /// <summary>
        ///     Create a new instance of <see cref="PipelineExecutor" />.
        /// </summary>
        /// <param name="settings">The settings to read data from.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="busNotifications">Bus notifications.</param>
        public PipelineExecutor(ReadOnlySettings settings, IBuilder builder, BusNotifications busNotifications)
            : this(builder, busNotifications, settings.Get<PipelineModifications>(), builder.Build<BehaviorContextStacker>())
        {
        }

        internal PipelineExecutor(IBuilder builder, BusNotifications busNotifications, PipelineModifications pipelineModifications, BehaviorContextStacker contextStacker)
        {
            this.busNotifications = busNotifications;
            this.contextStacker = contextStacker;

            var coordinator = new StepRegistrationsCoordinator(pipelineModifications.Removals, pipelineModifications.Replacements);
            foreach (var rego in pipelineModifications.Additions)
            {
                coordinator.Register(rego);
            }

            Incoming = coordinator.BuildPipelineModelFor<IncomingContext>();
            Outgoing = coordinator.BuildPipelineModelFor<OutgoingContext>();

            incomingBehaviors = Incoming.Select(r => r.CreateBehavior(builder)).ToArray();
            outgoingBehaviors = Outgoing.Select(r => r.CreateBehavior(builder)).ToArray();
        }

        /// <summary>
        ///     The list of incoming steps registered.
        /// </summary>
        public IList<RegisterStep> Incoming { get; private set; }

        /// <summary>
        ///     The list of outgoing steps registered.
        /// </summary>
        public IList<RegisterStep> Outgoing { get; private set; }

        internal BehaviorContext InvokeSendPipeline(OutgoingContext context)
        {            
            return InvokePipeline(outgoingBehaviors, context);
        }
        
        internal void InvokeReceivePipeline(IncomingContext context)
        {
            try
            {
                InvokePipeline(incomingBehaviors, context);
            }
            catch (MessageProcessingAbortedException)
            {
                //We swallow this one because it is used to signal aborting of processing.
            }
        }

        BehaviorContext InvokePipeline<TContext>(IEnumerable<BehaviorInstance> behaviors, TContext context) where TContext : BehaviorContext
        {
            var pipeline = new BehaviorChain(behaviors, context, this, busNotifications);
            return pipeline.Invoke(contextStacker);
        }


        readonly BehaviorContextStacker contextStacker;
        IEnumerable<BehaviorInstance> incomingBehaviors;
        IEnumerable<BehaviorInstance> outgoingBehaviors;
        readonly BusNotifications busNotifications;
    }
}
