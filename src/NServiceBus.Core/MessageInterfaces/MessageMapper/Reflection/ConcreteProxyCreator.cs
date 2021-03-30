namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    class ConcreteProxyCreator
    {
        public ConcreteProxyCreator()
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("NServiceBusMessageProxies"), AssemblyBuilderAccess.Run);
            moduleBuilder = assemblyBuilder.DefineDynamicModule("NServiceBusMessageProxies");
        }

        /// <summary>
        /// Generates the concrete implementation of the given type.
        /// Only properties on the given type are generated in the concrete implementation.
        /// </summary>
        public Type CreateTypeFrom(Type type)
        {
            var typeBuilder = moduleBuilder.DefineType(type.FullName + SUFFIX,
                TypeAttributes.Serializable | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed,
                typeof(object)
                );

            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            foreach (var prop in GetAllProperties(type))
            {
                var propertyType = prop.PropertyType;

                var fieldBuilder = typeBuilder.DefineField(
                    "field_" + prop.Name,
                    propertyType,
                    FieldAttributes.Private);

                var propBuilder = typeBuilder.DefineProperty(
                    prop.Name,
                    prop.Attributes | PropertyAttributes.HasDefault,
                    propertyType,
                    null);

                foreach (var customAttribute in prop.GetCustomAttributesData())
                {
                    AddCustomAttributeToProperty(customAttribute, propBuilder);
                }

                var getMethodBuilder = typeBuilder.DefineMethod(
                    "get_" + prop.Name,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.VtableLayoutMask,
                    propertyType,
                    Type.EmptyTypes);

                var getIL = getMethodBuilder.GetILGenerator();
                // For an instance property, argument zero is the instance. Load the
                // instance, then load the private field and return, leaving the
                // field value on the stack.
                getIL.Emit(OpCodes.Ldarg_0);
                getIL.Emit(OpCodes.Ldfld, fieldBuilder);
                getIL.Emit(OpCodes.Ret);

                // Define the "set" accessor method for Number, which has no return
                // type and takes one argument of type int (Int32).
                var setMethodBuilder = typeBuilder.DefineMethod(
                    "set_" + prop.Name,
                    getMethodBuilder.Attributes,
                    null,
                    new[]
                    {
                        propertyType
                    });

                var setIL = setMethodBuilder.GetILGenerator();
                // Load the instance and then the numeric argument, then store the
                // argument in the field.
                setIL.Emit(OpCodes.Ldarg_0);
                setIL.Emit(OpCodes.Ldarg_1);
                setIL.Emit(OpCodes.Stfld, fieldBuilder);
                setIL.Emit(OpCodes.Ret);

                // Last, map the "get" and "set" accessor methods to the
                // PropertyBuilder. The property is now complete.
                propBuilder.SetGetMethod(getMethodBuilder);
                propBuilder.SetSetMethod(setMethodBuilder);
            }

            typeBuilder.AddInterfaceImplementation(type);

            return typeBuilder.CreateTypeInfo().AsType();
        }

        /// <summary>
        /// Given a custom attribute and property builder, adds an instance of the custom attribute
        /// to the property builder
        /// </summary>
        static void AddCustomAttributeToProperty(CustomAttributeData attributeData, PropertyBuilder propBuilder)
        {
            var namedArguments = attributeData.NamedArguments;

            object[] constructorArgs = attributeData.ConstructorArguments.Select(ExtractValue).ToArray();
            if (namedArguments == null)
            {
                var attributeBuilder = new CustomAttributeBuilder(
                    attributeData.Constructor,
                    constructorArgs);

                propBuilder.SetCustomAttribute(attributeBuilder);
            }
            else
            {
                PropertyInfo[] namedProperties = namedArguments.Select(x => (PropertyInfo)x.MemberInfo).ToArray();
                object[] propertyValues = namedArguments.Select(x => x.TypedValue.Value).ToArray();

                var attributeBuilder = new CustomAttributeBuilder(
                    attributeData.Constructor,
                    constructorArgs,
                    namedProperties,
                    propertyValues);

                propBuilder.SetCustomAttribute(attributeBuilder);
            }
        }

        static object ExtractValue(CustomAttributeTypedArgument arg)
        {
            if (arg.Value is ReadOnlyCollection<CustomAttributeTypedArgument> nestedValue)
            {
                return nestedValue.Select(x => x.Value).ToArray();
            }
            return arg.Value;
        }

        /// <summary>
        /// Returns all properties on the given type, going up the inheritance hierarchy.
        /// </summary>
        static IEnumerable<PropertyInfo> GetAllProperties(Type type)
        {
            var props = new List<PropertyInfo>(type.GetProperties());
            foreach (var interfaceType in type.GetInterfaces())
            {
                props.AddRange(GetAllProperties(interfaceType));
            }

            var tracked = new List<PropertyInfo>(props.Count);
            var duplicates = new List<PropertyInfo>(props.Count);
            foreach (var p in props)
            {
                var duplicate = tracked.SingleOrDefault(n => n.Name == p.Name && n.PropertyType == p.PropertyType);
                if (duplicate != null)
                {
                    duplicates.Add(p);
                }
                else
                {
                    tracked.Add(p);
                }
            }

            foreach (var d in duplicates)
            {
                props.Remove(d);
            }

            return props;
        }

        ModuleBuilder moduleBuilder;
        internal const string SUFFIX = "__impl";
    }
}