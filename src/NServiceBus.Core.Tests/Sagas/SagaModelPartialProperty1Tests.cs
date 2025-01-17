namespace NServiceBus.Core.Tests.Sagas.TypeBasedSagas;

public partial class MyPartialEntity : ContainSagaData
{

#pragma warning disable IDE0051 // Remove unused private members
    int _uniqueProperty;
#pragma warning restore IDE0051 // Remove unused private members

    public partial int UniqueProperty { get; set; }


}


