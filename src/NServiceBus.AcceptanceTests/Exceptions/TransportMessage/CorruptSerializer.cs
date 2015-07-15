namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class CorruptSerializer : Behavior<TransportReceiveContext>
    {
        public override void Invoke(TransportReceiveContext context, Action next)
        {
            var corrupter = SerializerCorrupter.Corrupt();
            context.Set(corrupter);
        }

        internal class Registration : RegisterStep
        {
            public Registration()
                : base("CorruptSerializer", typeof(CorruptSerializer), "Corrupts the standard serializer")
            {
                InsertBefore("ReceiveMessage");
            }
        }
    }
}