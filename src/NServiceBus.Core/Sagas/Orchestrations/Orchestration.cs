namespace NServiceBus.Sagas.Orchestrations
{
    using System.Threading.Tasks;

    /// <summary>
    /// The orchestration using async-await infrastructure to
    /// </summary>
    public abstract class Orchestration<TStartingMessage> : SagaBase, IAmStartedByMessages<TStartingMessage>
    {
        /// <inheritdoc />
        protected internal override void ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Runs the orchestration.
        /// </summary>
        /// <param name="input">The initial input parameter.</param>
        /// <param name="ctx">The context.</param>
        /// <returns>A task.</returns>
        protected abstract Task Run(TStartingMessage input, IOrchestrationContext ctx);

        Task IHandleMessages<TStartingMessage>.Handle(TStartingMessage message, IMessageHandlerContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}