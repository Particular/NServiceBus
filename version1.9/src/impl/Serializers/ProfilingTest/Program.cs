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
            Class1 tester = new Class1();
            tester.TestInterfaces();

            Console.ReadLine();
        }
    }
}
