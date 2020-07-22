namespace NServiceBus
{
    using System.Text;

    static class JsonPrettyPrinter
    {
        const string LINE_INDENT = "  ";
        
        internal static string Print(string input)
        {
            var builder = new StringBuilder(input.Length);
            var quoted = false;
            var indent = 0;

            for (var i = 0; i < input.Length; i++)
            {
                var ch = input[i];
                switch (ch)
                {
                    case '{':
                    case '[':
                        builder.Append(ch);
                        if (!quoted) PrintIndent(builder, ++indent);
                        break;
                    case '}':
                    case ']':
                        if(!quoted) PrintIndent(builder, --indent);
                        builder.Append(ch);
                        break;
                    case '"':
                        builder.Append(ch);
                        var escaped = IsEscaped(input, i);
                        if (!escaped) quoted = !quoted;
                        break;
                    case ',':
                        builder.Append(ch);
                        if (!quoted) PrintIndent(builder, indent);
                        break;
                    case ':':
                        builder.Append(ch);
                        if (!quoted) builder.Append(" ");
                        break;
                    default:
                        builder.Append(ch);
                        break;
                }
            }

            return builder.ToString();
        }

        static bool IsEscaped(string input, int i)
        {
            var escaped = false;
            var index = i;
            while (index > 0 && input[--index] == '\\')
            {
                escaped = !escaped;
            }
            return escaped;
        }

        static void PrintIndent(StringBuilder sb, int indent)
        {
            sb.AppendLine();
            for (var i = 0; i < indent; i++)
            {
                sb.Append(LINE_INDENT);
            }
        }
    }
}