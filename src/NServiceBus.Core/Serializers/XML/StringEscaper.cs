namespace NServiceBus.Serializers.XML
{
    using System;
    using System.Text;

    class StringEscaper
    {

        public static string Escape(char c)
        {
            if (c == 0x9 || c == 0xA || c == 0xD
                || (0x20 <= c && c <= 0xD7FF)
                || (0xE000 <= c && c <= 0xFFFD)
                || (0x10000 <= c && c <= 0x10ffff)
                )
            {
                string ss = null;
                switch (c)
                {
                    case '<':
                        ss = "&lt;";
                        break;
                    case '>':
                        ss = "&gt;";
                        break;
                    case '"':
                        ss = "&quot;";
                        break;
                    case '\'':
                        ss = "&apos;";
                        break;
                    case '&':
                        ss = "&amp;";
                        break;
                }
                if (ss != null)
                {
                    return ss;
                }
            }
            else
            {
                return String.Format("&#x{0:X};", (int)c);
            }

            //Should not get here but just in case!
            return c.ToString();
        }

        public static string Escape(string stringToEscape)
        {
            if (string.IsNullOrEmpty(stringToEscape))
            {
                return stringToEscape;
            }

            StringBuilder builder = null; // initialize if we need it

            var startIndex = 0;
            for (var i = 0; i < stringToEscape.Length; ++i)
            {
                var c = stringToEscape[i];
                if (c == 0x9 || c == 0xA || c == 0xD
                    || (0x20 <= c && c <= 0xD7FF)
                    || (0xE000 <= c && c <= 0xFFFD)
                    || (0x10000 <= c && c <= 0x10ffff)
                    )
                {
                    string ss = null;
                    switch (c)
                    {
                        case '<':
                            ss = "&lt;";
                            break;
                        case '>':
                            ss = "&gt;";
                            break;
                        case '"':
                            ss = "&quot;";
                            break;
                        case '\'':
                            ss = "&apos;";
                            break;
                        case '&':
                            ss = "&amp;";
                            break;
                    }
                    if (ss != null)
                    {
                        if (builder == null)
                        {
                            builder = new StringBuilder(stringToEscape.Length + ss.Length);
                        }
                        if (startIndex < i)
                        {
                            builder.Append(stringToEscape, startIndex, i - startIndex);
                        }
                        startIndex = i + 1;
                        builder.Append(ss);
                    }

                }
                else
                {
                    // invalid characters
                    if (builder == null)
                    {
                        builder = new StringBuilder(stringToEscape.Length + 8);
                    }
                    if (startIndex < i)
                    {
                        builder.Append(stringToEscape, startIndex, i - startIndex);
                    }
                    startIndex = i + 1;
                    builder.AppendFormat("&#x{0:X};", (int)c);
                }
            }

            if (startIndex < stringToEscape.Length)
            {
                if (builder == null)
                {
                    return stringToEscape;
                }
                builder.Append(stringToEscape, startIndex, stringToEscape.Length - startIndex);
            }

            if (builder != null)
            {
                return builder.ToString();
            }

            //Should not get here but just in case!
            return stringToEscape;
        }
    }
}