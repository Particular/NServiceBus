namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using ObjectBuilder;
    using Unicast;

    public class UnitOfWorkBehavior : IBehavior
    {
        readonly IBuilder builder;
        public IBehavior Next { get; set; }

        public UnitOfWorkBehavior(IBuilder builder)
        {
            this.builder = builder;
        }

        public void Invoke(IBehaviorContext context)
        {
            var unitOfWorkRunner = new UnitOfWorkRunner {Builder = builder};

            try
            {
                unitOfWorkRunner.Begin();

                Next.Invoke(context);

                unitOfWorkRunner.End();
            }
            catch (Exception exception)
            {
                unitOfWorkRunner.AppendEndExceptionsAndRethrow(exception);
            }
        }
    }
}