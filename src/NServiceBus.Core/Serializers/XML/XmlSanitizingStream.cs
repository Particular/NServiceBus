namespace NServiceBus.Serializers.XML {
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
	/// A StreamReader that excludes XML-illegal characters while reading.
	/// </summary>
	public class XmlSanitizingStream : StreamReader
	{
		/// <summary>
		/// The character that denotes the end of a file has been reached.
		/// </summary>
		private const int EOF = -1;
	
		/// <summary>Create an instance of XmlSanitizingStream.</summary>
		/// <param name="streamToSanitize">
		/// The stream to sanitize of illegal XML characters.
		/// </param>
		public XmlSanitizingStream(Stream streamToSanitize)
			: base(streamToSanitize, true)
		{ }

        /// <summary>
        /// Get whether an integer represents a legal XML 1.0 or 1.1 character. See
        /// the specification at w3.org for these characters.
        /// </summary>
        /// <param name="xmlVersion">
        /// The version number as a string. Use "1.0" for XML 1.0 character
        /// validation, and use "1.1" for XML 1.1 character validation.
        /// </param>
		/// <returns><c>true</c> if is a legal xml character.</returns>
		public static bool IsLegalXmlChar(string xmlVersion, int character)
		{
			switch (xmlVersion)
			{
				case "1.1": // http://www.w3.org/TR/xml11/#charsets
				{
					return 
					!(
						character <= 0x8                       ||
						character == 0xB                       ||
						character == 0xC                       ||
						(character >= 0xE  && character<= 0x1F) ||
						(character >= 0x7F && character<= 0x84) ||
						(character >= 0x86 && character<= 0x9F) ||
						character >  0x10FFFF
					);
				}
				case "1.0": // http://www.w3.org/TR/REC-xml/#charsets
				{                    
					return
					(
						character == 0x9 /* == '\t' == 9   */          ||
						character == 0xA /* == '\n' == 10  */          ||
						character == 0xD /* == '\r' == 13  */          ||
						(character >= 0x20    && character <= 0xD7FF  ) ||
						(character >= 0xE000  && character <= 0xFFFD  ) ||
						(character >= 0x10000 && character <= 0x10FFFF)
					);
				}
				default:
				{
					throw new ArgumentOutOfRangeException
                        ("xmlVersion", string.Format("'{0}' is not a valid XML version.", xmlVersion));
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
	
		} // method
	
		#region Read*() method overrides
		
		// The following methods are exact copies of the methods in TextReader, 
		// extracting by disassembling it in Reflector
	
		public override int Read(char[] buffer, int index, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			if ((buffer.Length - index) < count)
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
	
		#endregion
	
	} // class

}
