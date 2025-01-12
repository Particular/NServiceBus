namespace NServiceBus.Core.Tests.Sagas.TypeBasedSagas;

public partial class MyPartialEntity : ContainSagaData
{

    int _uniqueProperty;

    public partial int UniqueProperty { get; set; }


}


