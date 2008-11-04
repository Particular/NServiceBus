using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NServiceBus.Serializers.InterfacesToXML.Test;
using System.Reflection;

namespace NServiceBus.Serializers.InterfacesToXML.XsdGenerator
{
    class Program
    {
        private string nameSpace = "http://tempuri.net/";

        static void Main(string[] args)
        {
        }

        public void Do()
        {
            Events.GuidDetected += delegate
                                       {
                                           needToGenerateGuid = true;
                                       };

            TopLevelScan(typeof(IM2));
            TopLevelScan(typeof(M2));

            GenerateXsd();
        }

        private void GenerateXsd()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            builder.AppendFormat(
                "<xs:schema elementFormDefault=\"qualified\" xmlns=\"{0}\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">", nameSpace);

            if (needToGenerateGuid)
                builder.AppendLine("<xs:import namespace=\"http://microsoft.com/wsdl/types/\" />");

            foreach (ComplexType complex in Repository.ComplexTypes)
                builder.Append(ComplexTypeWriter.Write(complex));

            builder.AppendLine("</xs:schema>");

            string result = builder.ToString();
        }

        public void TopLevelScan(Type type)
        {
            if (typeof(IMessage).IsAssignableFrom(type))
                Scan(type);
        }

        public void Scan(Type type)
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

        private bool needToGenerateGuid;
    }
}
