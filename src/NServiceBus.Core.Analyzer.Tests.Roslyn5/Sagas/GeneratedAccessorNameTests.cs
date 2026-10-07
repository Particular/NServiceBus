namespace NServiceBus.Core.Analyzer.Tests.Sagas;

using System.Linq;
using NUnit.Framework;
using static Analyzer.Sagas.Sagas;

[TestFixture]
public class GeneratedAccessorNameTests
{
    [Test]
    public void Message_accessors_whose_parts_join_to_the_same_text_get_different_identities_and_names()
    {
        PropertyMappingSpec[] mappings =
        [
            Mapping("global::Order_Id", "Order_Id", "Id"),
            Mapping("global::Order", "Order", "Id_Id"),
            Mapping("global::Order_", "Order_", "Id"),
            Mapping("global::Order", "Order", "_Id"),
            Mapping("global::Start", "Start", "Id", accessedMember: "global::Base.Id|cast=global::Derived"),
            Mapping("global::Start", "Start", "Id", accessedMember: "global::Base.Id", castType: "global::Derived"),
            Mapping("global::Start", "Start", "Id", externReceiverType: "global::Start", externMethodName: "Outer.get_Id"),
            Mapping("global::Start", "Start", "Id", externReceiverType: "global::Start.Outer", externMethodName: "get_Id")
        ];

        var identities = mappings.Select(Emitter.MessagePropertyAccessorIdentity.Of).ToArray();
        var names = mappings.Select(Emitter.MessagePropertyAccessorName).ToArray();

        Assert.That(identities, Is.Unique);
        Assert.That(names, Is.Unique);
    }

    [Test]
    public void Correlation_accessors_whose_parts_join_to_the_same_text_get_different_identities_and_names()
    {
        (string SagaDataType, CorrelationPropertyMappingSpec Mapping)[] mappings =
        [
            ("global::Data_string", Correlation("Prop")),
            ("global::Data", Correlation("string_Prop")),
            ("global::Data_", Correlation("Prop")),
            ("global::Data", Correlation("_Prop")),
            ("global::Data", Correlation("Prop", "global::Order_Id")),
            ("global::Data", Correlation("Id_Prop", "global::Order")),
            ("global::Data", Correlation("Prop", externGetterReceiverType: "global::Data"))
        ];

        var identities = mappings.Select(m => Emitter.CorrelationPropertyAccessorIdentity.Of(m.SagaDataType, m.Mapping)).ToArray();
        var names = mappings.Select(m => Emitter.CorrelationPropertyAccessorName(m.SagaDataType, m.Mapping)).ToArray();

        Assert.That(identities, Is.Unique);
        Assert.That(names, Is.Unique);
    }

    static PropertyMappingSpec Mapping(string messageType, string messageName, string propertyName, string accessedMember = null, string castType = null, string externReceiverType = null, string externMethodName = null) =>
        new(messageType, messageName, propertyName, "string", castType, externReceiverType, externMethodName, false, accessedMember, ImmutableEquatableArray<string>.Empty);

    static CorrelationPropertyMappingSpec Correlation(string propertyName, string propertyType = "string", string externGetterReceiverType = null) =>
        new(propertyName, propertyType, propertyType, externGetterReceiverType, null, false, ImmutableEquatableArray<string>.Empty, ImmutableEquatableArray<string>.Empty);
}
