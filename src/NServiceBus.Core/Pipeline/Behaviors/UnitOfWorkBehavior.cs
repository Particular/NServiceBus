namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using ObjectBuilder;
    using Unicast;

    class UnitOfWorkBehavior : IBehavior
    {
        public IBuilder Builder { get; set; }

        public void Invoke(BehaviorContext context, Action next)
        {
            var unitOfWorkRunner = new UnitOfWorkRunner
            {
                Builder = Builder
            };

            try
            {
                context.Trace("Starting uow");
                unitOfWorkRunner.Begin();

                next();

                context.Trace("Ending uow");
                unitOfWorkRunner.End();
            }
            catch (Exception exception)
            {
                context.Trace("Appending exception: {0}", exception);
                unitOfWorkRunner.AppendEndExceptionsAndRethrow(exception);
            }
        }
    }
}