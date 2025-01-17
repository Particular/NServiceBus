namespace NServiceBus.Core.Tests.Sagas;

public partial class MyPartialEntity : ContainSagaData
{
#pragma warning disable IDE0032
    int _uniqueProperty;

    public partial int UniqueProperty
    {
        get
        {
            return _uniqueProperty;
        }
        set
        {
            _uniqueProperty = value;
        }
    }
#pragma warning restore IDE0032
}


