namespace NServiceBus.PersistenceTesting
{
    using System;

    public class TestVariant
    {
        public object[] Values { get; }

        public TimeSpan? SessionTimeout { get; set; }

        public TestVariant(params object[] values) => Values = values;

        public override string ToString() => string.Join(" ", Values);
    }
}