namespace NServiceBus.Serializers.XML.XsdGenerator
{
    using System.Text;

    public class ElementWriter
    {
        Element e;
        string startFormat = "<xs:element minOccurs=\"{0}\" maxOccurs=\"";
        string nameFormat = "\" name=\"{0}\" ";
        string namespaceFormat = "xmlns:" + Strings.NamespacePrefix + "=\"{0}\" ";
        string typeFormat = "type=\"{0}\" />\n";

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
            var builder = new StringBuilder();

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
