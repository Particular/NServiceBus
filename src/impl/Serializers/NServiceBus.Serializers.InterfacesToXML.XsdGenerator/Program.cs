using System;
using System.IO;
using System.Text;
using System.Reflection;

namespace NServiceBus.Serializers.InterfacesToXML.XsdGenerator
{
    class Program
    {
        private static string nameSpace = "http://tempuri.net/";

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Assembly a = Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, args[0]));
            if (args.Length == 2)
                nameSpace = args[1];

            Events.GuidDetected += delegate
                                       {
                                           needToGenerateGuid = true;
                                       };

            foreach(Type t in a.GetTypes())
                TopLevelScan(t);

            string xsd = GenerateXsdString();

            using(StreamWriter writer = File.CreateText(GetFileName()))
                writer.Write(xsd);

            if (needToGenerateGuid)
                using (StreamWriter writer = File.CreateText(GetFileName()))
                    writer.Write(Strings.GuidXsd);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("Usage: first parameter [required], your assembly; second parameter [optional], your namespace.");
        }

        private static string GetFileName()
        {
            int i = 0;
            while (File.Exists(string.Format("schema{0}.xsd", i)))
                i++;

            return string.Format("schema{0}.xsd", i);
        }

        private static string GenerateXsdString()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            builder.AppendFormat(
                "<xs:schema elementFormDefault=\"qualified\" xmlns=\"{0}\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">\n", nameSpace);

            if (needToGenerateGuid)
                builder.AppendLine("<xs:import namespace=\"http://microsoft.com/wsdl/types/\" />");

            foreach (ComplexType complex in Repository.ComplexTypes)
                builder.Append(ComplexTypeWriter.Write(complex));

            foreach (Type simple in Repository.SimpleTypes)
                builder.Append(SimpleTypeWriter.Write(simple));

            builder.AppendLine("</xs:schema>");

            string result = builder.ToString();

            return result;
        }

        public static void TopLevelScan(Type type)
        {
            if (typeof(IMessage).IsAssignableFrom(type))
                Scan(type);
        }

        public static void Scan(Type type)
        {
            if (type == null || type == typeof(object) || type == typeof(IMessage))
                return;

            Repository.Handle(type);

            if (!type.IsInterface)
                Scan(type.BaseType);
            else
                foreach (Type i in type.GetInterfaces())
                    Scan(i);
        }

        private static bool needToGenerateGuid;
    }
}
