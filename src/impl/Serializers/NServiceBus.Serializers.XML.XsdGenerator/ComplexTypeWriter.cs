using System.Text;

namespace NServiceBus.Serializers.InterfacesToXML.XsdGenerator
{
    public class ComplexTypeWriter
    {
        private readonly ComplexType complex;
        private readonly string beginTopFormat = "<xs:complexType name=\"{0}\">\n";
        private readonly string beginBaseFormat = "<xs:complexContent mixed=\"false\">\n<xs:extension base=\"{0}\">\n";
        private readonly string beginSequence = "<xs:sequence>";
        private readonly string endSequence = "</xs:sequence>";
        private readonly string endBaseFormat = "</xs:extension>\n</xs:complexContent>\n";
        private readonly string endTopFormat = "</xs:complexType>\n";

        public static string Write(ComplexType complex)
        {
            return new ComplexTypeWriter(complex).Write();
        }

        private ComplexTypeWriter(ComplexType complex)
        {
            this.complex = complex;
        }

        public string Write()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendFormat(beginTopFormat, complex.Name);

            if (complex.BaseName != null)
                builder.AppendFormat(beginBaseFormat, complex.BaseName);

            builder.AppendLine(beginSequence);

            foreach (Element e in complex.Elements)
                builder.AppendFormat(ElementWriter.Write(e));

            builder.AppendLine(endSequence);

            if (complex.BaseName != null)
                builder.AppendFormat(endBaseFormat);

            builder.AppendFormat(endTopFormat);

            return builder.ToString();
        }
    }
}
