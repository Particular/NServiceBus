namespace NServiceBus.Core.Tests.Sagas;

public partial class MyPartialEntity : ContainSagaData
{        
#pragma warning disable IDE0055
    public partial int UniqueProperty { get; set; }
#pragma warning restore IDE0055
}


