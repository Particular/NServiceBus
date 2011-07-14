using System;
using System.Collections.Generic;
using System.Text;
using NServiceBus.Serializers.XML.Test;

namespace ProfilingTest
{
    class Program
    {
        static void Main(string[] args)
        {
            SerializerTests tester = new SerializerTests();
            tester.TestInterfaces();

            Console.ReadLine();
        }
    }
}
