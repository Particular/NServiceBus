
namespace NServiceBus.Serializers.XML.XsdGenerator
{
    public static class Strings
    {
        public static readonly string ArrayOf = "ArrayOf";
        public static readonly string NamespacePrefix = "q1";

        public static readonly string GuidXsd = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                                                "<xs:schema xmlns:tns=\"http://microsoft.com/wsdl/types/\" elementFormDefault=\"qualified\" targetNamespace=\"http://microsoft.com/wsdl/types/\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">\n" +
                                                "\t<xs:simpleType name=\"guid\">\n" +
                                                "\t\t<xs:restriction base=\"xs:string\">\n" +
                                                "\t\t\t<xs:pattern value=\"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\" />\n" +
                                                "\t\t</xs:restriction>\n" +
                                                "\t</xs:simpleType>\n" +
                                                "</xs:schema>\n";
    }
}
