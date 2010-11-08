using System;
using System.Text;

namespace NServiceBus.Serializers.XML.XsdGenerator
{
    public class SimpleTypeWriter
    {
        public static void Write(Type t, StringBuilder builder)
        {
            if (!t.IsEnum)
                return;

            builder.AppendFormat("<xs:simpleType name=\"{0}\">\n", t.Name);
            builder.AppendLine("<xs:restriction base=\"xs:string\">");

            foreach(string val in Enum.GetNames(t))
                builder.AppendFormat("<xs:enumeration value=\"{0}\" />\n", val);

            builder.AppendLine("</xs:restriction>");
            builder.AppendLine("</xs:simpleType>");
        }

        public static void WriteChar(StringBuilder builder)
        {
            builder.Append("<xs:simpleType name=\"Char\">\n<xs:restriction base=\"xs:string\">\n<xs:minLength value=\"0\"/>\n<xs:maxLength value=\"1\"/>\n</xs:restriction>\n</xs:simpleType>\n");
        }
    }
}
