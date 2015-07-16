namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class CorruptSerializer : Behavior<IncomingContext>
    {
        public override void Invoke(IncomingContext context, Action next)
        {
            var corrupter = SerializerCorrupter.Corrupt();
            context.Set(corrupter);

            next();
        }

        internal class Registration : RegisterStep
        {
            public Registration()
                : base("CorruptSerializer", typeof(CorruptSerializer), "Corrupts the standard serializer")
            {
                InsertBeforeIfExists("ReceiveMessage");
            }
        }
    }
}