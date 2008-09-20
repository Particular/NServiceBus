using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NServiceBus.MessageInterfaces.MessageMapper.Reflection
{
    public class MessageMapper : IMessageMapper
    {
        public void Initialize(params Type[] types)
        {
            if (types == null || types.Length == 0)
                return;

            string name = types[0].Namespace + SUFFIX;

            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName(name),
                AssemblyBuilderAccess.RunAndSave
                );

            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(name, name + ".dll");

            foreach (Type t in types)
            {
                if (t.IsInterface)
                {
                    Type mapped = CreateTypeFrom(t, moduleBuilder);
                    TypeMapping[t] = mapped;
                }

                nameToType[t.FullName] = t;
            }

            assemblyBuilder.Save(name + ".dll");
        }

        public Type CreateTypeFrom(Type t, ModuleBuilder moduleBuilder)
        {
            TypeBuilder typeBuilder = moduleBuilder.DefineType(
                t.Namespace + SUFFIX + "." + t.Name,
                TypeAttributes.Serializable | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed,
                typeof(object)
                );

            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            foreach (PropertyInfo prop in t.GetProperties())
            {
                Type propertyType = prop.PropertyType;
                nameToType[propertyType.FullName] = propertyType;

                FieldBuilder fieldBuilder = typeBuilder.DefineField(
                    "field_" + prop.Name,
                    propertyType,
                    FieldAttributes.Private);

                PropertyBuilder propBuilder = typeBuilder.DefineProperty(
                    prop.Name,
                    prop.Attributes | PropertyAttributes.HasDefault,
                    propertyType,
                    null);

                MethodBuilder getMethodBuilder = typeBuilder.DefineMethod(
                    "get_" + prop.Name,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.VtableLayoutMask,
                    propertyType,
                    Type.EmptyTypes);

                ILGenerator getIL = getMethodBuilder.GetILGenerator();
                // For an instance property, argument zero is the instance. Load the 
                // instance, then load the private field and return, leaving the
                // field value on the stack.
                getIL.Emit(OpCodes.Ldarg_0);
                getIL.Emit(OpCodes.Ldfld, fieldBuilder);
                getIL.Emit(OpCodes.Ret);

                // Define the "set" accessor method for Number, which has no return
                // type and takes one argument of type int (Int32).
                MethodBuilder setMethodBuilder = typeBuilder.DefineMethod(
                    "set_" + prop.Name,
                    getMethodBuilder.Attributes,
                    null,
                    new Type[] { propertyType });

                ILGenerator setIL = setMethodBuilder.GetILGenerator();
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

            typeBuilder.AddInterfaceImplementation(t);

            return typeBuilder.CreateType();
        }

        public Type GetMappedTypeFor(Type t)
        {
            if (t.IsClass)
            {
                if (t.Namespace.EndsWith(SUFFIX))
                {
                    string s = t.Namespace.Replace(SUFFIX, "") + "." + t.Name;
                    return GetMappedTypeFor(s);
                }

                return t;
            }

            if (TypeMapping.ContainsKey(t))
                return TypeMapping[t];

            return null;
        }

        public Type GetMappedTypeFor(string typeName)
        {
            if (nameToType.ContainsKey(typeName))
                return nameToType[typeName];

            return null;
        }

        public T CreateInstance<T>() where T : IMessage
        {
            return (T)CreateInstance(typeof(T));
        }

        public object CreateInstance(Type t)
        {
            return Activator.CreateInstance(GetMappedTypeFor(t));
        }

        private static readonly string SUFFIX = ".__Impl";
        private static readonly Dictionary<Type, Type> TypeMapping = new Dictionary<Type, Type>();
        private static readonly Dictionary<string, Type> nameToType = new Dictionary<string, Type>();

    }
}
