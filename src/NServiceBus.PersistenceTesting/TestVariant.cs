namespace NServiceBus.PersistenceTesting
{
    public class TestVariant
    {
        public object[] Values { get; }

        public TestVariant(params object[] values)
        {
            Values = values;
        }

        public override string ToString()
        {
            return string.Join(" ", Values);
        }
    }
}