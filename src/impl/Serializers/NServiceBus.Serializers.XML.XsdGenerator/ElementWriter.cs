using System.Text;

namespace NServiceBus.Serializers.XML.XsdGenerator
{
    public class ElementWriter
    {
        private readonly Element e;
        private readonly string startFormat = "<xs:element minOccurs=\"{0}\" maxOccurs=\"";
        private readonly string nameFormat = "\" name=\"{0}\" ";
        private readonly string namespaceFormat = "xmlns:" + Strings.NamespacePrefix + "=\"{0}\" ";
        private readonly string typeFormat = "type=\"{0}\" />\n";

        public static string Write(Element e)
        {
            return new ElementWriter(e).Write();
        }

        private ElementWriter(Element e)
        {
            this.e = e;
        }

        public string Write()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendFormat(startFormat, e.MinOccurs);

            if (e.UnboundedMaxOccurs)
                builder.Append("unbounded");
            else
                builder.Append("1");

            builder.AppendFormat(nameFormat, e.Name);

            if (e.Nillable)
                builder.Append("nillable=\"true\" ");

            if (e.NameSpace != null)
                builder.AppendFormat(namespaceFormat, e.NameSpace);

            builder.AppendFormat(typeFormat, e.Type);

            return builder.ToString();
        }
    }
}
