namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class UncorruptSerializer : Behavior<TransportReceiveContext>
    {
        public override void Invoke(TransportReceiveContext context, Action next)
        {
            var restorer = context.Get<SerializerCorrupter.Restorer>();
            restorer.Dispose();

            next();
        }

        internal class Registration : RegisterStep
        {
            public Registration()
                : base("UncorruptSerializer", typeof(UncorruptSerializer), "Restores the corrupted standard serializer")
            {
            }
        }
    }
}