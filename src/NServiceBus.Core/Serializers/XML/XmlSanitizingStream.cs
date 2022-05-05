namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Text;

    // A StreamReader that excludes XML-illegal characters while reading.
    class XmlSanitizingStream : StreamReader
    {
        public XmlSanitizingStream(Stream streamToSanitize)
            : base(streamToSanitize, true)
        {
        }

        public static bool IsLegalXmlChar(string xmlVersion, int character)
        {
            switch (xmlVersion)
            {
                case "1.1": // http://www.w3.org/TR/xml11/#charsets
                    {
                        return
                                character is not (<= 0x8 or
                                0xB or
                                0xC or
                                (>= 0xE and <= 0x1F) or
                                (>= 0x7F and <= 0x84) or
                                (>= 0x86 and <= 0x9F) or
                                > 0x10FFFF)
;
                    }
                case "1.0": // http://www.w3.org/TR/REC-xml/#charsets
                    {
                        return
                            character is 0x9 /* == '\t' == 9   */or
                            0xA /* == '\n' == 10  */or
                            0xD /* == '\r' == 13  */or
                            (>= 0x20 and <= 0xD7FF) or
                            (>= 0xE000 and <= 0xFFFD) or
                            (>= 0x10000 and <= 0x10FFFF);
                    }
                default:
                    {
                        throw new ArgumentOutOfRangeException
                            (nameof(xmlVersion), $"'{xmlVersion}' is not a valid XML version.");
                    }
            }
        }

        /// <summary>
        /// Get whether an integer represents a legal XML 1.0 character. See the
        /// specification at w3.org for these characters.
        /// </summary>
        public static bool IsLegalXmlChar(int character)
        {
            return IsLegalXmlChar("1.0", character);
        }

        public override int Read()
        {
            // Read each character, skipping over characters that XML has prohibited
            int nextCharacter;

            do
            {
                // Read a character
                if ((nextCharacter = base.Read()) == EOF)
                {
                    // If the character denotes the end of the file, stop reading
                    break;
                }
            }

            // Skip the character if it's prohibited, and try the next
            while (!IsLegalXmlChar(nextCharacter));

            return nextCharacter;
        }

        public override int Peek()
        {
            // Return the next legal XML character without reading it
            int nextCharacter;

            do
            {
                // See what the next character is
                nextCharacter = base.Peek();
            }
            while
                (
                // If it's prohibited XML, skip over the character in the stream
                // and try the next.
                !IsLegalXmlChar(nextCharacter) &&
                (nextCharacter = base.Read()) != EOF
                );

            return nextCharacter;
        }

        // The following methods are exact copies of the methods in TextReader,
        // extracting by disassembling it in Reflector
        public override int Read(char[] buffer, int index, int count)
        {
            Guard.AgainstNull(nameof(buffer), buffer);
            Guard.AgainstNegative(nameof(index), index);
            Guard.AgainstNegative(nameof(count), count);

            if (buffer.Length - index < count)
            {
                throw new ArgumentException();
            }
            var number = 0;
            do
            {
                var nextNumber = Read();
                if (nextNumber == -1)
                {
                    return number;
                }
                buffer[index + number++] = (char)nextNumber;
            }
            while (number < count);

            return number;
        }

        public override int ReadBlock(char[] buffer, int index, int count)
        {
            int number;
            var nextNumber = 0;
            do
            {
                nextNumber += number = Read(buffer, index + nextNumber, count - nextNumber);
            }
            while ((number > 0) && (nextNumber < count));

            return nextNumber;
        }

        public override string ReadLine()
        {
            var builder = new StringBuilder();
            while (true)
            {
                var number = Read();
                switch (number)
                {
                    case -1:
                        if (builder.Length > 0)
                        {
                            return builder.ToString();
                        }
                        return null;

                    case 13:
                    case 10:
                        if ((number == 13) && (Peek() == 10))
                        {
                            Read();
                        }
                        return builder.ToString();

                    default:
                        break;
                }
                builder.Append((char)number);
            }
        }

        public override string ReadToEnd()
        {
            int number;
            var buffer = new char[0x1000];
            var builder = new StringBuilder(0x1000);
            while ((number = Read(buffer, 0, buffer.Length)) != 0)
            {
                builder.Append(buffer, 0, number);
            }
            return builder.ToString();
        }

        const int EOF = -1;
    }
}