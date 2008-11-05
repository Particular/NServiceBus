using System;
using System.Text;

namespace NServiceBus.Serializers.InterfacesToXML.XsdGenerator
{
    public class SimpleTypeWriter
    {
        public static string Write(Type t)
        {
            if (!t.IsEnum)
                return string.Empty;

            StringBuilder builder = new StringBuilder();

            builder.AppendFormat("<xs:simpleType name=\"{0}\">\n", t.Name);
            builder.AppendLine("<xs:restriction base=\"xs:string\">");

            foreach(string val in Enum.GetNames(t))
                builder.AppendFormat("<xs:enumeration value=\"{0}\" />\n", val);

            builder.AppendLine("</xs:restriction>");
            builder.AppendLine("</xs:simpleType>");

            return builder.ToString();
        }
    }
}
