namespace NServiceBus.Serializers.XML.XsdGenerator
{
    using System.Text;

    public class ComplexTypeWriter
    {
        ComplexType complex;
        string beginTopFormat = "<xs:complexType name=\"{0}\">\n";
        string beginBaseFormat = "<xs:complexContent mixed=\"false\">\n<xs:extension base=\"{0}\">\n";
        string beginSequence = "<xs:sequence>";
        string endSequence = "</xs:sequence>";
        string endBaseFormat = "</xs:extension>\n</xs:complexContent>\n";
        string endTopFormat = "</xs:complexType>\n";

        public static void Write(ComplexType complex, StringBuilder builder)
        {
            new ComplexTypeWriter(complex).Write(builder);
        }

        private ComplexTypeWriter(ComplexType complex)
        {
            this.complex = complex;
        }

        public void Write(StringBuilder builder)
        {
            builder.AppendFormat(beginTopFormat, complex.Name);

            if (complex.BaseName != null)
                builder.AppendFormat(beginBaseFormat, complex.BaseName);

            builder.AppendLine(beginSequence);

            foreach (var e in complex.Elements)
                builder.AppendFormat(ElementWriter.Write(e));

            builder.AppendLine(endSequence);

            if (complex.BaseName != null)
                builder.AppendFormat(endBaseFormat);

            builder.AppendFormat(endTopFormat);
        }
    }
}
