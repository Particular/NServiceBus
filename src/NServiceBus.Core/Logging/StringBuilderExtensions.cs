namespace NServiceBus
{
    using System;
    using System.Text;

    static class StringBuilderExtensions
    {
        /// <summary>
        /// Appends a new line marker to the StringBuilder followed by the text in the <paramref name="newLine"/>.
        /// </summary>
        public static StringBuilder NewLine(this StringBuilder stringBuilder, string newLine)
        {
            return stringBuilder.Append(Environment.NewLine).Append(newLine);
        }
    }
}