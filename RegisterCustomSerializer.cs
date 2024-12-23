using System;
using System.Collections.Generic;

using System.Reflection;

namespace ENIX
{
    public static class RegisterCustomSerializer
    {
        private static Dictionary<Type, MethodInfo>? s_RegisteredCustomPropertySerializer;
        private static Dictionary<Type, MethodInfo>? s_RegisteredCustomPropertyDeserializer;

        public static void Register()
        {
            s_RegisteredCustomPropertySerializer = new Dictionary<Type, MethodInfo>();
            s_RegisteredCustomPropertyDeserializer = new Dictionary<Type, MethodInfo>();

            Assembly assembly = Assembly.GetExecutingAssembly();

            foreach (Type type in assembly.GetTypes())
            {
                if (Attribute.GetCustomAttribute(type, typeof(CustomSerializer)) == null)
                    continue;

                MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (MethodInfo method in methods)
                {
                    CustomPropertySerializerMethod? propertySerializerMethod = (CustomPropertySerializerMethod?)method
                        .GetCustomAttribute(typeof(CustomPropertySerializerMethod));

                    CustomPropertyDeserializerMethod? propertyDeserializerMethod = (CustomPropertyDeserializerMethod?)method
                        .GetCustomAttribute(typeof(CustomPropertyDeserializerMethod));

                    if (propertySerializerMethod != null
                        && propertyDeserializerMethod != null)
                    {
                        continue;
                    }

                    if (propertySerializerMethod != null)
                    {
                        Type objType = propertySerializerMethod.Type;
                        s_RegisteredCustomPropertySerializer.Add(objType, method);
                    }
                    else if (propertyDeserializerMethod != null)
                    {
                        Type objType = propertyDeserializerMethod.Type;
                        s_RegisteredCustomPropertyDeserializer.Add(objType, method);
                    }
                }
            }
        }

        public static bool TryGetPropertySerializeMethod(Type propertyType, out MethodInfo method)
        {
            method = null;

            if (s_RegisteredCustomPropertySerializer.ContainsKey(propertyType) == false)
                return false;

            method = s_RegisteredCustomPropertySerializer[propertyType];
            return true;
        }

        public static bool TryGetPropertyDeserializeMethod(Type propertyType, out MethodInfo method)
        {
            method = null;

            if (s_RegisteredCustomPropertyDeserializer.ContainsKey(propertyType) == false)
                return false;

            method = s_RegisteredCustomPropertyDeserializer[propertyType];
            return true;
        }
    }
}
