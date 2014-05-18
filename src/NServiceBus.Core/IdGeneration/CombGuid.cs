namespace NServiceBus.IdGeneration
{
    using System;

    /// <summary>
    /// Generates <see cref="System.Guid"/> values 
    /// using a strategy suggested Jimmy Nilsson's 
    /// <a href="http://www.informit.com/articles/article.asp?p=25862">article</a>
    /// on <a href="http://www.informit.com">informit.com</a>. 
    /// </summary>
    /// <remarks>
    /// <p>
    /// The <c>comb</c> algorithm is designed to make the use of <see cref="Guid"/>s as Primary Keys, Foreign Keys, 
    /// and Indexes nearly as efficient as ints.
    /// </p>
    /// <p>
    /// This code was modified based on Donald Mull's contributor to the
    /// NHibernate source.
    /// </p>
    /// </remarks>
    public static class CombGuid
    {
        /// <summary>
        /// Generate a new <see cref="Guid"/> using the comb algorithm.
        /// </summary>
        public static Guid Generate()
        {
            var guidArray = Guid.NewGuid().ToByteArray();

            var baseDate = new DateTime(1900, 1, 1);
            var now = DateTime.Now;

            // Get the days and milliseconds which will be used to build the byte string 
            var days = new TimeSpan(now.Ticks - baseDate.Ticks);
            var timeOfDay = now.TimeOfDay;

            // Convert to a byte array 
            // Note that SQL Server is accurate to 1/300th of a millisecond so we divide by 3.333333 
            var daysArray = BitConverter.GetBytes(days.Days);
            var millisecondArray = BitConverter.GetBytes((long)(timeOfDay.TotalMilliseconds / 3.333333));

            // Reverse the bytes to match SQL Servers ordering 
            Array.Reverse(daysArray);
            Array.Reverse(millisecondArray);

            // Copy the bytes into the guid 
            Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
            Array.Copy(millisecondArray, millisecondArray.Length - 4, guidArray, guidArray.Length - 4, 4);

            return new Guid(guidArray);
        }
    }
}

