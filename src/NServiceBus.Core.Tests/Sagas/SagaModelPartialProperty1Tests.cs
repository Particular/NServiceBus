namespace NServiceBus.Core.Tests.Sagas.TypeBasedSagas;

public partial class MyPartialEntity : ContainSagaData
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0032:Use auto property", Justification = "Manual getter/setter provides control over property behavior.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "Field is part of a partial property implementation.")]

    int _uniqueProperty;

    public partial int UniqueProperty { get; set; }

}


