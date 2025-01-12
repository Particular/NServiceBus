namespace NServiceBus.Core.Tests.Sagas.TypeBasedSagas;

public partial class MyPartialEntity : ContainSagaData
{

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
}


